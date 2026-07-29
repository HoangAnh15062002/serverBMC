try {
    $excel = New-Object -ComObject Excel.Application
    $excel.Visible = $false
    $excel.DisplayAlerts = $false
    
    $wb2 = $excel.Workbooks.Open("D:\BMC\ServerBMC\ServerBMC\1. Mong M02B (5,6,8,9).xls")
    
    # Sheet "Tong hop VT" (index 4)
    Write-Host "`n=== Sheet 4: Tong hop VT ===" 
    $ws4 = $wb2.Sheets.Item(4)
    Write-Host ("Rows=" + $ws4.UsedRange.Rows.Count + ", Cols=" + $ws4.UsedRange.Columns.Count)
    for ($r = 1; $r -le [math]::Min(40, $ws4.UsedRange.Rows.Count); $r++) {
        $line = ""
        for ($c = 1; $c -le [math]::Min(12, $ws4.UsedRange.Columns.Count); $c++) {
            $val = $ws4.Cells.Item($r, $c).Text
            if ($val -and $val.Trim() -ne "") {
                $line = $line + "C" + $c + "=" + $val + " | "
            }
        }
        if ($line -ne "") {
            Write-Host ("Row" + $r + ": " + $line)
        }
    }
    
    # Sheet "Nhan cong" (index 5)
    Write-Host "`n`n=== Sheet 5: Nhan cong ===" 
    $ws5 = $wb2.Sheets.Item(5)
    Write-Host ("Rows=" + $ws5.UsedRange.Rows.Count + ", Cols=" + $ws5.UsedRange.Columns.Count)
    for ($r = 1; $r -le [math]::Min(40, $ws5.UsedRange.Rows.Count); $r++) {
        $line = ""
        for ($c = 1; $c -le [math]::Min(10, $ws5.UsedRange.Columns.Count); $c++) {
            $val = $ws5.Cells.Item($r, $c).Text
            if ($val -and $val.Trim() -ne "") {
                $line = $line + "C" + $c + "=" + $val + " | "
            }
        }
        if ($line -ne "") {
            Write-Host ("Row" + $r + ": " + $line)
        }
    }
    
    # Sheet "May" (index 6)
    Write-Host "`n`n=== Sheet 6: May ===" 
    $ws6 = $wb2.Sheets.Item(6)
    Write-Host ("Rows=" + $ws6.UsedRange.Rows.Count + ", Cols=" + $ws6.UsedRange.Columns.Count)
    for ($r = 1; $r -le [math]::Min(40, $ws6.UsedRange.Rows.Count); $r++) {
        $line = ""
        for ($c = 1; $c -le [math]::Min(10, $ws6.UsedRange.Columns.Count); $c++) {
            $val = $ws6.Cells.Item($r, $c).Text
            if ($val -and $val.Trim() -ne "") {
                $line = $line + "C" + $c + "=" + $val + " | "
            }
        }
        if ($line -ne "") {
            Write-Host ("Row" + $r + ": " + $line)
        }
    }
    
    # Sheet "TH chi phi XD" (index 15)
    Write-Host "`n`n=== Sheet 15: TH chi phi XD ===" 
    $ws15 = $wb2.Sheets.Item(15)
    Write-Host ("Rows=" + $ws15.UsedRange.Rows.Count + ", Cols=" + $ws15.UsedRange.Columns.Count)
    for ($r = 1; $r -le [math]::Min(40, $ws15.UsedRange.Rows.Count); $r++) {
        $line = ""
        for ($c = 1; $c -le [math]::Min(15, $ws15.UsedRange.Columns.Count); $c++) {
            $val = $ws15.Cells.Item($r, $c).Text
            if ($val -and $val.Trim() -ne "") {
                $line = $line + "C" + $c + "=" + $val + " | "
            }
        }
        if ($line -ne "") {
            Write-Host ("Row" + $r + ": " + $line)
        }
    }
    
    $wb2.Close($false)
    $excel.Quit()
    [System.Runtime.Interopservices.Marshal]::ReleaseComObject($excel) | Out-Null
}
catch {
    Write-Host "Error: $_"
}
