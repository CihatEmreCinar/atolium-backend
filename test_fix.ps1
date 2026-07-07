$email = "emre@employer.com"
$password = "Test@123"
$baseUrl = "http://localhost:5178/api/v1"

Write-Host "Testing with account: $email" -ForegroundColor Cyan

$loginBody = @{
    email = $email
    password = $password
} | ConvertTo-Json

$loginResp = Invoke-WebRequest -Uri "$baseUrl/auth/login" -Method POST -ContentType "application/json" -Body $loginBody
$loginData = $loginResp.Content | ConvertFrom-Json
$accessToken = $loginData.accessToken

Write-Host "Logged in successfully" -ForegroundColor Green

$headers = @{ "Authorization" = "Bearer $accessToken" }

$workshopsResp = Invoke-WebRequest -Uri "$baseUrl/workshops/user" -Method GET -Headers $headers
$workshops = $workshopsResp.Content | ConvertFrom-Json

if ($workshops.Count -gt 0) {
    $workshop = $workshops[0]
    $workshopId = $workshop.id
    Write-Host "Found workshop: $($workshop.title)" -ForegroundColor Cyan
    
    $postBody = @{
        workshopId = $workshopId
        caption = "Test post from previously failing account"
    } | ConvertTo-Json
    
    $postResp = Invoke-WebRequest -Uri "$baseUrl/posts" -Method POST -ContentType "application/json" -Headers $headers -Body $postBody -SkipHttpErrorCheck
    
    if ($postResp.StatusCode -eq 201) {
        Write-Host "SUCCESS! Post created (201)" -ForegroundColor Green
        $postData = $postResp.Content | ConvertFrom-Json
        Write-Host "Post ID: $($postData.id)"
    } else {
        Write-Host "FAILED! Status: $($postResp.StatusCode)" -ForegroundColor Red
        Write-Host $postResp.Content
    }
}
