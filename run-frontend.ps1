if (-not (Test-Path -Path ".\frontend\.env")) {
    Write-Host "❌ Missing frontend\.env"
    exit 1
}

Write-Host "Installing dependencies..."
cd frontend
npm install

Write-Host "Starting frontend..."
npm run dev
