 = "c:\Project\Game-server\src\Message\Packet"
 = Get-ChildItem -Path  -Directory

foreach ( in ) {
     = Get-ChildItem -Path .FullName -Filter "*RequestPacket.cs"
    foreach ( in ) {
         = .Name -replace "RequestPacket\.cs", ""
         = "ResponsePacket.cs"
         = Join-Path .FullName 

        if (Test-Path ) {
            Write-Host "Merging  in "
             = Get-Content .FullName
             = Get-Content 

             = @()
             = @()

            foreach ( in ) {
                if ( -match "^\s*using ") {
                    if ( -notcontains ) {  +=  }
                } else {
                     += 
                }
            }

            foreach ( in ) {
                if ( -match "^\s*using ") {
                    if ( -notcontains ) {  +=  }
                } else {
                     += 
                }
            }

             =  + "
" + 
             = "Packets.cs"
             = Join-Path .FullName 

            Set-Content -Path  -Value  -Encoding UTF8

            Remove-Item .FullName
            Remove-Item 
        }
    }
}
Write-Host "Merge completed."
