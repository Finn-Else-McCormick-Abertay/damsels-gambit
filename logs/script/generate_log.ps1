# generate_log.ps1

# Generate log in csv format
git log --pretty=format:"%h,%an,%ad,&!&%(decorate)&!&,%x22%s%x22" --date=format:'%b %d %H:%M' | Tee-Object -Variable log | Out-Null

# Clean up usernames
$log = $log -replace "beaparsons", "Bea Parsons"
$log = $log -replace "h-wthorn|THORN STIRLING", "Thorn Stirling"
$log = $log -replace "Finn-Else-McCormick-Abertay", "Finn Else-McCormick"

# Reformat decorate step
$log = $log -replace '&!&\s?(?:(?:tag: (?<tag>[^,\)]*))|.)*&!&', '${tag}'

# Save to file
$date = Get-Date -Format "dd/MM/yyyy HH-mm-ss"
$log | Out-File -FilePath "${(Get-Item .).FullName}generated-logs/Commit History $date.csv" -Encoding ascii -Force
