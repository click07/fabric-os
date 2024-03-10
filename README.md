# fabric-os
A 32-Bit Operating System written in **URCL**

## Features ##
- Multiprocessing
- Graphic / Audio Ports
- Custom FS
- Basic IL (Similiar to URCL)
- Installer (URCX Version only)

## Run fabricOS ##
To run fabricOS you can either compile the source file to an instruction set, which is supported by your CPU or you can use an emulator for URCL like URCL Explorer by BramOtte (https://bramotte.github.io/urcl-explorer) to directly run the OS in your web browser. FabricOS requires a storage file which can be downloaded from this repository. Alternatively, you can insert an empty file (containing only 0s) with a recommended size of ~1KB and let the installer create a basic installation of fabricOS. The amount of storage available is determined by the size of your storage file.

## Repository Structure ##
-  ./os
Operating System Source Files (URCL/URCX)
- ./fs
Full Storage Files
- ./tools
Development and other useful external tools.
  - Yarn (Oxided Output to FabricIL)
  - Fabricator (Upload and Merge Files into a FabricOS Storage File)
- ./prg
Internal Program Source Files.
### Branches ###
- main - Stable Build
- dev - Just there to show progress; Most likely non-functional

## Development ##
### Contributing to the Repo ###
Create a pull request if you have improvements or fixes.
### Developing Programs ###
For program development you can use the higher level language "Oxided" by Em3rald1 (https://github.com/em3rald1/oxided) and use the transpiler included in ./tools/yarn (requires dotnet) to transpile the urcl output to fabricIL. 
*No Libraries are available for the language though, stay patient.*

## Resources ##
- Spreadsheets:
https://docs.google.com/spreadsheets/d/1X-0zKYvePeubFpfF74jwvh0A-KQrocQ5c1mMpyZt1w0/edit?usp=sharing
