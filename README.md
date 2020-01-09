# new! brief Manual
## <span style="color: pink; ">This is a beta version. Probably the code will change!</span>
# Sudoku_Solver_Generator and Shoot with Camera
![GNPX](/images/Sudoku_Camera00.png)

 Sudoku analysis and generation C# program.
 The only algorithm used is non-backtracking.
 The algorithm used is
   Single, LockedCandidate, (hidden)LockedSet(2D-7D),
   (Finned)(Franken/Mutant/Kraken)Fish(2D-7D),
   Skyscraper, EmptyRectangle, XY-Wing, W-Wing, RemotePair, XChain, XYChain,
   SueDeCoq, (Multi)Coloring,
   ALS-Wing, ALS-XZ, ALS-Chain,
   (ALS)DeathBlossom(Ext.), (Grouped)NiceLoop, ForceChain and 
   GeneralLogic.<br>
There are also functions for transposed transformation of Sudoku problems, standardization and ordering of Sudoku problems.
The algorithm is explained on the HTML page:<br>
  https://gidoo-code.github.io/Sudoku_Solver_Generator/index.html<br>
  https://gidoo-code.github.io/Sudoku_Solver_Generator/page2.html

## This version can shoot sudoku with web_camera
Two files(_LMparameter.txt,_0123456789Arial.jpg) are required for execution. These are in the "SUDOKUCamera_App" folder. 
If the executable file is not in "SUDOKU Camera App", compile and generate from the project(
because under development).


## Another Sample
Recognition of distorted image.
![GNPX](/images/Sudoku_Camera01.png)  

Recognition of  uneven brightness image.
![GNPX](/images/Sudoku_Camera02.png)
