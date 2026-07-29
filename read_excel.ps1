try {
    $excel = New-Object -ComObject Excel.Application
    $excel.Visible = $false
    $excel.DisplayAlerts = $false
    
    # File 1
    $wb1 = $excel.Workbooks.Open("D:\BMC\ServerBMC\ServerBMC\0. Bia_M-02B (5,6,8,9).xlsx")
    $ws1 = $wb1.Sheets.Item(1)
    $range1 = $ws1.UsedRange
    
    Write-Host "=== FILE 1: Bia_M-02B ===" 
    Write-Host "Sheet: $($ws1.Name)"
    Write-Host "Rows: $($range1.Rows.Count), Cols: $($range1.Columns.Count)"
    Write-Host ""
    
    for ($r = 1; $r -le [math]::Min(30, $range1.Rows.Count); $r++) {
        $row = @()
        for ($c = 1; $c -le $range1.Columns.Count; $c++) {
            $cell = $ws1.Cells.Item($r, $c)
            $row += $cell.Text
        }
        Write-Host "Row $r : " ($row -join " | ")
    }
    
    $wb1.Close($false)
    
    # File 2
    Write-Host ""
    Write-Host "=== FILE 2: Mong_M02B ===" 
    if (Test-Path "D:\BMC\ServerBMC\ServerBMC\1. Mong M02B (5,6,8,9).xls") {
        $wb2 = $excel.Workbooks.Open("D:\BMC\ServerBMC\ServerBMC\1. Mong M02B (5,6,8,9).xls")
        $ws2 = $wb2.Sheets.Item(1)
        $range2 = $ws2.UsedRange
        
        Write-Host "Sheet: $($ws2.Name)"
        Write-Host "Rows: $($range2.Rows.Count), Cols: $($range2.Columns.Count)"
        Write-Host ""
        
        for ($r = 1; $r -le [math]::Min(30, $range2.Rows.Count); $r++) {
            $row = @()
            for ($c = 1; $c -le $range2.Columns.Count; $c++) {
                $cell = $ws2.Cells.Item($r, $c)
                $row += $cell.Text
            }
            Write-Host "Row $r : " ($row -join " | ")
        }
        
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
