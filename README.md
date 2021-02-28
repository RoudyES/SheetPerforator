# Sheet Perforator
Import an image and perforate it then export into DXF format.
The program will process the image and detect pixel intensities and based on those intensities it will generate circles with different size. The circle sizes will always be within a specified range.

# Installer
If you want to just use the software without cloning this repo and having to build the code, you can download it's installer from [this link](https://drive.google.com/file/d/1tG6sH6ABYcbgLlAGqkKiMr5KCvVMWCGo/view?usp=sharing)

# Example Output

The software will import an image from disk and the preferred configuration values have to be filled.
![alt text](https://github.com/RoudyES/SheetPerforator/blob/master/SheetPerforator/Example1.PNG?raw=true)

Next, clicking the process button will process the image and generate the circles and when finished, it will show a preview image of the result.
![alt text](https://github.com/RoudyES/SheetPerforator/blob/master/SheetPerforator/Example2.PNG?raw=true)

Finally, click export to generate the DXF file and open it with AutoCAD or your preferred software to see the final result output.
![alt text](https://github.com/RoudyES/SheetPerforator/blob/master/SheetPerforator/Example3.PNG?raw=true)


# References
This software was written in C# using the WPF Framework.<br/>
The image processing part used [EmguCV](http://www.emgu.com/wiki/index.php/Main_Page).<br/>
Exporting into DXF format was done using [netDxf](https://github.com/haplokuon/netDxf) package.
