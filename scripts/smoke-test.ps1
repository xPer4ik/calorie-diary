param(
    [string]$BaseUrl = "http://localhost:5010",
    [string]$BackendProject = ".\backend"
)

$ErrorActionPreference = "Stop"
$startedProcess = $null
$outputLog = Join-Path $env:TEMP "calorie-diary-smoke-test.out.log"
$errorLog = Join-Path $env:TEMP "calorie-diary-smoke-test.err.log"

function Write-Pass {
    param([string]$Message)
    Write-Host "PASS $Message" -ForegroundColor Green
}

function Write-Fail {
    param([string]$Message)
    Write-Host "FAIL $Message" -ForegroundColor Red
}

function Invoke-Api {
    param(
        [string]$Method,
        [string]$Uri,
        $Body = $null,
        $Headers = $null
    )

    $params = @{
        Method = $Method
        Uri = $Uri
        TimeoutSec = 10
        UseBasicParsing = $true
    }

    if ($null -ne $Body) {
        $params.ContentType = "application/json"
        $params.Body = ($Body | ConvertTo-Json -Compress)
    }

    if ($null -ne $Headers) {
        $params.Headers = $Headers
    }

    try {
        $response = Invoke-WebRequest @params
        $content = if ([string]::IsNullOrWhiteSpace($response.Content)) {
            $null
        } else {
            $response.Content | ConvertFrom-Json
        }

        return [pscustomobject]@{
            StatusCode = [int]$response.StatusCode
            Body = $content
        }
    }
    catch [System.Net.WebException] {
        $response = $_.Exception.Response

        if ($null -eq $response) {
            throw
        }

        $reader = [System.IO.StreamReader]::new($response.GetResponseStream())
        $text = $reader.ReadToEnd()
        $content = if ([string]::IsNullOrWhiteSpace($text)) {
            $null
        } else {
            $text | ConvertFrom-Json
        }

        return [pscustomobject]@{
            StatusCode = [int]$response.StatusCode
            Body = $content
        }
    }
}

function Test-ApiReady {
    try {
        $health = Invoke-Api -Method "GET" -Uri "$BaseUrl/api/health"
        return $health.StatusCode -eq 200
    }
    catch {
        return $false
    }
}

function Assert-Status {
    param(
        $Response,
        [int]$Expected,
        [string]$Name
    )

    if ($Response.StatusCode -ne $Expected) {
        throw "$Name returned status $($Response.StatusCode), expected $Expected."
    }

    Write-Pass $Name
}

try {
    Remove-Item -LiteralPath $outputLog, $errorLog -ErrorAction SilentlyContinue

    if (-not (Test-ApiReady)) {
        Write-Host "Backend is not running. Starting backend at $BaseUrl..."
        $env:ASPNETCORE_ENVIRONMENT = "Development"
        $startedProcess = Start-Process `
            -FilePath "dotnet" `
            -ArgumentList @("run", "--urls", $BaseUrl) `
            -WorkingDirectory (Resolve-Path $BackendProject) `
            -RedirectStandardOutput $outputLog `
            -RedirectStandardError $errorLog `
            -PassThru `
            -WindowStyle Hidden

        $ready = $false

        for ($i = 0; $i -lt 30; $i++) {
            if (Test-ApiReady) {
                $ready = $true
                break
            }

            Start-Sleep -Milliseconds 700
        }

        if (-not $ready) {
            throw "Backend did not become ready in time. Logs: $outputLog $errorLog"
        }
    }

    $health = Invoke-Api -Method "GET" -Uri "$BaseUrl/api/health"
    Assert-Status -Response $health -Expected 200 -Name "GET /api/health"

    $stamp = [DateTimeOffset]::UtcNow.ToUnixTimeMilliseconds()
    $email = "smoke-$stamp@example.com"
    $password = "secret123"

    $register = Invoke-Api -Method "POST" -Uri "$BaseUrl/api/auth/register" -Body @{
        email = $email
        password = $password
        displayName = "Smoke Test"
    }
    Assert-Status -Response $register -Expected 201 -Name "POST /api/auth/register"

    if ([string]::IsNullOrWhiteSpace($register.Body.token)) {
        throw "Register response did not contain a token."
    }

    $login = Invoke-Api -Method "POST" -Uri "$BaseUrl/api/auth/login" -Body @{
        email = $email
        password = $password
    }
    Assert-Status -Response $login -Expected 200 -Name "POST /api/auth/login"

    if ([string]::IsNullOrWhiteSpace($login.Body.token)) {
        throw "Login response did not contain a token."
    }

    $headers = @{
        Authorization = "Bearer $($login.Body.token)"
    }

    $me = Invoke-Api -Method "GET" -Uri "$BaseUrl/api/auth/me" -Headers $headers
    Assert-Status -Response $me -Expected 200 -Name "GET /api/auth/me"

    if ($me.Body.email -ne $email) {
        throw "GET /api/auth/me returned unexpected email '$($me.Body.email)'."
    }

    $profile = Invoke-Api -Method "PUT" -Uri "$BaseUrl/api/profile" -Headers $headers -Body @{
        gender = "male"
        age = 30
        heightCm = 180
        weightKg = 80
        activityLevel = "moderate"
        goal = "maintain"
    }
    Assert-Status -Response $profile -Expected 200 -Name "PUT /api/profile"

    $foods = Invoke-Api -Method "GET" -Uri "$BaseUrl/api/foods" -Headers $headers
    Assert-Status -Response $foods -Expected 200 -Name "GET /api/foods"

    if (@($foods.Body).Count -lt 1) {
        throw "GET /api/foods returned no products."
    }

    $food = @($foods.Body | Select-Object -First 1)[0]
    $date = Get-Date -Format "yyyy-MM-dd"

    $diaryEntry = Invoke-Api -Method "POST" -Uri "$BaseUrl/api/diary" -Headers $headers -Body @{
        date = $date
        mealType = "breakfast"
        foodItemId = $food.id
        grams = 150
    }
    Assert-Status -Response $diaryEntry -Expected 201 -Name "POST /api/diary"

    if ($diaryEntry.Body.calories -le 0) {
        throw "POST /api/diary returned a non-positive calorie value."
    }

    $summary = Invoke-Api -Method "GET" -Uri "$BaseUrl/api/diary/summary?date=$date" -Headers $headers
    Assert-Status -Response $summary -Expected 200 -Name "GET /api/diary/summary"

    if ($summary.Body.calories -le 0) {
        throw "GET /api/diary/summary returned a non-positive calorie total."
    }

    Write-Host "Smoke test passed." -ForegroundColor Green
}
catch {
    Write-Fail $_.Exception.Message
    exit 1
}
finally {
    if ($startedProcess -and -not $startedProcess.HasExited) {
        Stop-Process -Id $startedProcess.Id -Force
        $startedProcess.WaitForExit()
    }
}
