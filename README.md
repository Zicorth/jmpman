## jmpman

_jmpman_ (or _jumpman_) is a command-line tool useful for editing jmp files found in Luigi's Mansion.
It works by converting to and fro binary and XML formats.

This program is licensed under the MIT license (see [here](LICENSE) for more information).

### Usage

As _jmpman_ is simply a converter between binary jmp files and XML jmp files, the only command-line options are to specify the input and output file names and formats:

```
jmpman.exe <xml|jmp> "<input file>" ["<output file>"]
```

The first argument sets the input file format (the output file format is then assumed to be the opposite choice).
The following argument specifies the path (relative or absolute) to the input file.
You can also optionally specify your own  output file name.
(By default, the output file name is the input file name with the output format appended as an extension.)

### Compiling

Like all of my tools, you'll need [arookas library](https://github.com/arookas/arookas) to compile and use this program.
To make building _jmpman_ easier, a [premake5](https://premake.github.io/) script is provided that generates a solution.
Just run the script through _premake5_ and you it should be ready to go.
