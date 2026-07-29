try {
    $excel = New-Object -ComObject Excel.Application
    $excel.Visible = $false
    $excel.DisplayAlerts = $false
    $excel.Workbooks.UseAutomationLatestVersion = $true
    
    # File 1
    Write-Host "=== FILE 1: Bia_M-02B ===" 
    $wb1 = $excel.Workbooks.Open("D:\BMC\ServerBMC\ServerBMC\0. Bia_M-02B (5,6,8,9).xlsx")
    
    Write-Host "Sheets: " $wb1.Sheets.Count
    for ($s = 1; $s -le $wb1.Sheets.Count; $s++) {
        Write-Host "Sheet $s : " $wb1.Sheets.Item($s).Name
    }
    
    $ws1 = $wb1.Sheets.Item(1)
    Write-Host "`n=== Sheet: " $ws1.Name " ===" 
    Write-Host "UsedRange: Rows=" $ws1.UsedRange.Rows.Count ", Cols=" $ws1.UsedRange.Columns.Count
    
    # Get all non-empty cells
    Write-Host "`n=== Non-empty cells ==="
    $nonEmpty = @()
    for ($r = 1; $r -le $ws1.UsedRange.Rows.Count; $r++) {
        for ($c = 1; $c -le 30; $c++) {
            $val = $ws1.Cells.Item($r, $c).Text
            if ($val -and $val.Trim() -ne "") {
                $nonEmpty += "R$r C$c: $val"
            }
        }
    }
    $nonEmpty | ForEach-Object { Write-Host $_ }
    
    $wb1.Close($false)
    
    # File 2
    Write-Host "`n=== FILE 2: Mong_M02B ===" 
    if (Test-Path "D:\BMC\ServerBMC\ServerBMC\1. Mong M02B (5,6,8,9).xls") {
        $wb2 = $excel.Workbooks.Open("D:\BMC\ServerBMC\ServerBMC\1. Mong M02B (5,6,8,9).xls")
        
        Write-Host "Sheets: " $wb2.Sheets.Count
        for ($s = 1; $s -le $wb2.Sheets.Count; $s++) {
            Write-Host "Sheet $s : " $wb2.Sheets.Item($s).Name
        }
        
        $ws2 = $wb2.Sheets.Item(1)
        Write-Host "`n=== Sheet: " $ws2.Name " ===" 
        Write-Host "UsedRange: Rows=" $ws2.UsedRange.Rows.Count ", Cols=" $ws2.UsedRange.Columns.Count
        
        # Get all non-empty cells
        Write-Host "`n=== Non-empty cells ==="
        $nonEmpty2 = @()
        for ($r = 1; $r -le $ws2.UsedRange.Rows.Count; $r++) {
            for ($c = 1; $c -le 30; $c++) {
                $val = $ws2.Cells.Item($r, $c).Text
                if ($val -and $val.Trim() -ne "") {
                    $nonEmpty2 += "R$r C$c: $val"
                }
            }
        }
        $nonEmpty2 | ForEach-Object { Write-Host $_ }
        
        $wb2.Close($false)
    } else {
        Write-Host "File 2 not found"
    }
    
    $excel.Quit()
    [System.Runtime.Interopservices.Marshal]::ReleaseComObject($excel) | Out-Null
}
catch {
    Write-Host "Error: $_"
}
