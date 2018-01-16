# SnowPlow

SnowPlow is a unit test adaptor for Visual Studio 2012+ for integrating igloo unit tests into the Unit Test Explorer.
In theory it should would with bandit, however that has not been tested.

## Features

### Out of the box
 ☑ Display unit test as success or fail in the Unit Test Explorer  
 ☑ Display error messages and location in the code where the assert occurred  
 ☑ Debug unit tests by right clicking on them in the Unit Test Explorer  
 ☑ Enabling / disabling unit test containers (.exe files) using configuration files  
 ☑ Customise environments for each unit test container with support for expanding environment variables (allows for PATH modifications)  
 ☑ Group unit tests by the root level context/describe  
 ☑ Replace '::' and '_' in unit tests with spaces for better readability  

### Feature that require extensions to igloo
 ☑ Double click on a unit test to open it  
 ☑ Group unit tests by project  
 ☐ Run/debug individual unit tests (limited by present capabilities of igloo)  

## Icon

The icon used for SnowPlow is from http://icons8.com and is an .ico file comprising the free sizes of the icon at ic8.link/17128

## Development

Opening this vs project requires that the "Microsoft Visual Studio 2013 SDK" is installed.
