Flex Scheduler README

------OPENING THE SOURCE CODE-------
1. Requires Microsoft Visual Studio 2013 or newer
2. Open FlexScheduler.sln file
3. There are three projects in the solution file:
	a. FlexScheduler: the core of the program
	b. FlexSchedulerConsoleTest: the console project to test/debug the program
	c. Web: the project containing the Web API
4. Build and run FlexSchedulerConsoleTest project to try the program

------ALTERNATIVE: Run the precompiled code-------
1. Navigate to Root > FlexSchedulerConsoleTest > bin > Release
2. Open the FlexScheduler.config to adjust the program
	a. Settings.Test.NumberOfReplicates, change this number to run multiple tests at the same time
3. All outputs are saved to the output folder