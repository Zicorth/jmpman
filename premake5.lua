workspace "jmpman"
	configurations { "Debug", "Release" }
	targetdir "bin/%{cfg.buildcfg}"
	
	filter "configurations:Debug"
		defines { "DEBUG" }
		flags { "Symbols" }
	
	filter "configurations:Release"
		defines { "RELEASE" }
		optimize "On"
	
	project "jmpman"
		kind "ConsoleApp"
		language "C#"
		namespace "arookas"
		location "jmpman"
		
		links {
			"arookas",
			"System",
			"System.Xml",
			"System.Xml.Linq"
		}
		
		files {
			"jmpman/**.cs",
		}
		
		excludes {
			"jmpman/bin/**",
			"jmpman/obj/**",
		}
