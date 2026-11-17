using CopperOS.Desktop.Build;

if (args.Length != 3) throw new ArgumentException("Usage: Desktop.Builder <qualified-provider-directory> <qualified-rom-directory> <new-output-directory>");
DesktopImageBuilder.Write(args[0], args[1], args[2]);
