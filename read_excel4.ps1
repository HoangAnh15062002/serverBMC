try {
    $excel = New-Object -ComObject Excel.Application
    $excel.Visible = $false
    $excel.DisplayAlerts = $false
    
    # File 2 - đọc tất cả sheets quan trọng
    Write-Host "=== FILE 2: Mong_M02B ===" 
    $wb2 = $excel.Workbooks.Open("D:\BMC\ServerBMC\ServerBMC\1. Mong M02B (5,6,8,9).xls")
    
    # Sheet "THKP hạng mục" (index 1)
    $ws = $wb2.Sheets.Item(1)
    Write-Host "`n=== Sheet 1: THKP hang muc ==="
    Write-Host "Rows=" $ws.UsedRange.Rows.Count ", Cols=" $ws.UsedRange.Columns.Count
    for ($r = 1; $r -le $ws.UsedRange.Rows.Count; $r++) {
        $line = ""
        for ($c = 1; $c -le $ws.UsedRange.Columns.Count; $c++) {
            $val = $ws.Cells.Item($r, $c).Text
            if ($val -and $val.Trim() -ne "") {
                $line = $line + "C" + $c + "=" + $val + " | "
            }
        }
        if ($line -ne "") {
            Write-Host ("Row" + $r + ": " + $line)
        }
    }
    
    # Sheet "Gia tong hop" (index 2)
    $ws2 = $wb2.Sheets.Item(2)
    Write-Host "`n`n=== Sheet 2: Gia tong hop ==="
    Write-Host "Rows=" $ws2.UsedRange.Rows.Count ", Cols=" $ws2.UsedRange.Columns.Count
    for ($r = 1; $r -le [math]::Min(30, $ws2.UsedRange.Rows.Count); $r++) {
        $line = ""
        for ($c = 1; $c -le [math]::Min(15, $ws2.UsedRange.Columns.Count); $c++) {
            $val = $ws2.Cells.Item($r, $c).Text
            if ($val -and $val.Trim() -ne "") {
                $line = $line + "C" + $c + "=" + $val + " | "
            }
        }
        if ($line -ne "") {
            Write-Host ("Row" + $r + ": " + $line)
        }
    }
    
    # Sheet "Don gia chi tiet" (index 3)
    $ws3 = $wb2.Sheets.Item(3)
    Write-Host "`n`n=== Sheet 3: Don gia chi tiet ==="
    Write-Host "Rows=" $ws3.UsedRange.Rows.Count ", Cols=" $ws3.UsedRange.Columns.Count
    for ($r = 1; $r -le [math]::Min(30, $ws3.UsedRange.Rows.Count); $r++) {
        $line = ""
        for ($c = 1; $c -le [math]::Min(15, $ws3.UsedRange.Columns.Count); $c++) {
            $val = $ws3.Cells.Item($r, $c).Text
            if ($val -and $val.Trim() -ne "") {
                $line = $line + "C" + $c + "=" + $val + " | "
            }
        }
        if ($line -ne "") {
            Write-Host ("Row" + $r + ": " + $line)
        }
    }
    
    $wb2.Close($false)
    
    # File 1 - Sheet "TM"
    Write-Host "`n`n=== FILE 1: Bia_M-02B - Sheets ===" 
    $wb1 = $excel.Workbooks.Open("D:\BMC\ServerBMC\ServerBMC\0. Bia_M-02B (5,6,8,9).xlsx")
    
    # Sheet TM (index 2)
    $wsBia = $wb1.Sheets.Item(2)
    Write-Host "`n=== Sheet TM ==="
    Write-Host "Rows=" $wsBia.UsedRange.Rows.Count ", Cols=" $wsBia.UsedRange.Columns.Count
    for ($r = 1; $r -le [math]::Min(50, $wsBia.UsedRange.Rows.Count); $r++) {
        $line = ""
        for ($c = 1; $c -le [math]::Min(20, $wsBia.UsedRange.Columns.Count); $c++) {
            $val = $wsBia.Cells.Item($r, $c).Text
            if ($val -and $val.Trim() -ne "") {
                $line = $line + "C" + $c + "=" + $val + " | "
            }
        }
        if ($line -ne "") {
            Write-Host ("Row" + $r + ": " + $line)
        }
    }
    
    $wb1.Close($false)
    
    $excel.Quit()
    [System.Runtime.Interopservices.Marshal]::ReleaseComObject($excel) | Out-Null
}
catch {
    Write-Host "Error: $_"
}
