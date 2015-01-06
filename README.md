Ini
===

Simple-to-use yet powerful Ini read and write system written in C#.

The goal of Ini is simply to be able to read, manipulate and write .ini files
in a simple manner as well as extending the capabilities of the ini format.

Ini is truly flexible and efficient as it is able to read files which are
separated both by a colon and an equals sign and spit out any of the both
in a readable mode where a person can easily edit the file, and a minimal mode
where where space is conserved. These settings is super easy to use and
makes sure Ini can write data that other software may use.

An example of how Ini works:

```c#
var ini = new Ini();

ini.SetString("Main", "Data", "Value");
ini.SetFloat("Main", "Float data", 0.0241f);

ini.SaveAs("output.ini");
```

This will create a new Ini object, add some values to it and write to a file
called `output.ini`. The following data will be written:

```ini
[Main]
Data = Value
Float data = 0.0241f
```

To read and retrieve data from the file, it's as simple as this:

```c#
var ini = new Ini("output.ini");

string data = ini.GetString("Main", "Data");
float floatData = ini.GetFloat("Main", "Float data");
```

To modify an existing file, simply load it in, apply some changes to the ini
object and save it again:

```c#
var ini = new Ini("output.ini");

ini.SetString("Main", "Data", "Changed value");
ini.SetString("Another section", "another property", "another value");

ini.Save();
```

Ini also has some awesome features to enumerate sections and properties, make
use of something called data blocks to store arbitrary data in ini files,
and more. I'll write about that later, but make sure to check out the source
if you want.
