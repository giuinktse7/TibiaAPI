# Get Tibia item names

## 1 Compilation

1. Open `TibiaAPI.sln` in Visual Studio
2. In `./Apps/GetItemNames/Program.cs:21`, update `_tibiaDirectory = "<path to your tibia installation>/packages/Tibia"` to match your tibia directory
3. Build the solution (change to Release mode for faster runtime execution)

## 2 Modifying the client to connect to our proxy

1. Open `<path to your tibia installation>/packages/Tibia/bin/client.exe` in a text editor
2. Replace the Tibia RSA

`BC27F992A96B8E2A43F4DFBE1CEF8FD51CF43D2803EE34FBBD8634D8B4FA32F7D9D9E159978DD29156D62F4153E9C5914263FC4986797E12245C1A6C4531EFE48A6F7C2EFFFFF18F2C9E1C504031F3E4A2C788EE96618FFFCEC2C3E5BFAFAF743B3FC7A872EE60A52C29AA688BDAF8692305312882F1F66EE9D8AEB7F84B1949`

with the OpenTibia RSA

`9B646903B45B07AC956568D87353BD7165139DD7940703B03E6DD079399661B4A837AA60561D7CCB9452FA0080594909882AB5BCA58A1A1B35F8B1059B72B1212611C6152AD3DBB3CFBEE7ADC142A75D3D75971509C321C5C24A5BD51FD460F01B4E15BEB0DE1930528A5D3F15C1E3CBF5C401D6777E10ACAAB33DBE8D5B7FF5`
3. Search for the line with `loginWebService` and replace it with

`loginWebService=http://127.0.0.1:7171/                               `

## 3 Generating the item name list

1. Start your tibia client using `<path to your tibia installation>/packages/Tibia/bin/client.exe --battleye`
2. From the project root, navigate to `./Apps/GetItemNames/bin/Release/netcoreapp3.1`
3. In that directory, run `./GetItemNames.exe`
4. Connect to a game world that is not BattlEye protected (Zuna/Zunera)
5. Write `send` in the terminal window
