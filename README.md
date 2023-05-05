# Voronoi-Delaunay
This project is a visualizer for Voronoi Diagrams and Delaunay Triangulations built using Unity. It was implemented as the final project for my Computational Geometry course at The University of South Florida.

This implementation uses the Bowyer-Watson Algorithm to generate the Delaunay Triangulation of the input points which can be generated randomly or inputted incrementally. More information about the algorithm can be found here: https://en.wikipedia.org/wiki/Bowyer%E2%80%93Watson_algorithm/

To generate the Voronoi Diagram of the input points, I use the dual relationship between the Delaunay Triangulation and Voronoi Diagram to generate the Voronoi vertices and edges from the previously calculated triangulation. The Voronoi vertices can be found by calculating the circumcenter of each Delaunay triangle. Circumcenters of neighboring triangles represent Voronoi edges.

# Visualizer Demo
https://user-images.githubusercontent.com/62521050/236524585-c50b83b3-6112-4d5a-9f0e-27b6273532f0.mp4

# Download Instructions
To install the demo application, download or clone this repository and open the *Voronoi_Delaunay_App* folder. Then run the executable. Your machine may flag the application as unsafe. If so, just ignore the message and run as administrator.

# Unity Instructions
If you would like to use this code in your own Unity Project, just copy the *Diagram.cs* and *ConvexHull.cs* files into your project. Then add *Diagram.cs* to a Game Object and fill in the prefab references in the script window. 

Point Prefab: Any Game Object or sprite to represent the input points\
Line Material V: Line material for Voronoi edges\
Line Material D: Line material for Delaunay edges\

![Script_Window](https://user-images.githubusercontent.com/62521050/236544775-74d46b9f-2440-4900-a184-ca80f2116be1.png)
