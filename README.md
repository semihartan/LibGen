# LibGen (Import Library Generator for Microsoft Visual C/C++ Compiler)

When you have to work with a non-MSVC compiler toolset such as MinGW's gcc or g++, you have dynamic link libraries without a corresponding import library file that is compatible with Microsoft's import library format. However, it is possible to create a Microsoft-compatible import library by creating a module definition file that contains the exported function names and their ordinals and inputting it to the Microsoft lib utility.

LibGen is a simple tool written in C# to automate these two steps and create an MSVC import file with a single command. I recommend you add LibGen to the `Path` environment variable.

## How To Use?
To generate an import file with LibGen, just give a DLL file by running it with the following syntax:
```
LibGen <Dll File Path>
```

It will detect the bitness of the DLL file and create the import library inside a sub-directory "lib\\x64" or "lib\\x86" according to the file's bitness.

## Requirements
This utility needs Microsoft lib utility to generate a lib file. Therefore, it also needs a Visual Studio installation in your system. If you don't have VS installed, you cannot use it. 
