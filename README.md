OpenSign — Execution Instructions
==================================

Description:
------------
This application enables secure digital document signing with placeholder support,
using RSA digital signatures and AES-encrypted private key storage.

How to Run (Windows):
---------------------
1. Navigate into the `publish/` folder included in this package.

2. Double-click the executable:

   > OpenSign.exe

   Alternatively, run it via terminal:

   > ./OpenSign.exe

3. Once the server is running, open your browser and go to:

   > http://localhost:5000
   or
   > http://localhost:5016
   (Check the terminal output for the exact port)

How to Run (with .NET CLI, cross-platform):
-------------------------------------------
If using Linux/macOS or running without the bundled `.exe`, use the .NET CLI:

1. Install the .NET SDK (https://dotnet.microsoft.com/download)
2. Run the application using the DLL:

   > dotnet OpenSign.dll

Notes:
------
- This is a self-contained publish build. The `.exe` should work out of the box on Windows 64-bit.
- Make sure no firewall or antivirus is blocking the application from opening the local port.
- For best results, use a modern browser (Chrome, Edge, Firefox).

