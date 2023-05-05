using System.Collections.Generic;
using UnityEngine;

public class Diagram : MonoBehaviour
{
    public GameObject menu; // Toolbar object
    public GameObject pointPrefab; // Prefab of the point object
    public Material lineMaterialV;
    public Material lineMaterialD;
    public int numPoints = 10; // Number of points to generate
    public float boundaryX = 5; // X-axis boundary of the game window
    public float boundaryY = 5; // Y-axis boundary of the game window

    private bool isDragging = false; // Flag to indicate if a point is being dragged
    private GameObject draggedPoint; // Reference to the point being dragged
    private bool DL = false; // Flag to indicate if the D key is pressed

    private List<GameObject> spawnedPoints = new List<GameObject>(); // List to store spawned points
    private List<GameObject> renderedLinesV = new List<GameObject>(); // List to store spawned lines
    private List<GameObject> renderedLinesD = new List<GameObject>(); // List to store spawned lines

    private List<Vector3> voronoiVertices = new List<Vector3>(); // List to store Voronoi vertices
    private List<LineSegment> voronoiEdges = new List<LineSegment>(); // List to store Voronoi edges
    private List<Triangle> delaunayEdges = new List<Triangle>(); // List to store Delaunay edges

    // Handle input and function updates every frame
    private void Update()
    {
        if (DL && spawnedPoints.Count > 2)
            GenerateDL();
        
        // Check for left mouse button click
        if (Input.GetMouseButtonDown(0))
        {
            // Get mouse position in world coordinates
            Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mousePosition.z = 0f;

            // Instantiate a point object at the mouse position
            GameObject point = Instantiate(pointPrefab, mousePosition, Quaternion.identity);

            // Set the point object as a child of the script's game object for organization
            point.transform.parent = transform;

            // Add the spawned point to the list of spawned points
            spawnedPoints.Add(point);
        }

        // Check for right mouse button down
        if (Input.GetMouseButtonDown(1))
        {
            // Cast a ray from the mouse position to detect if a point is being clicked
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction);
            if (hit.collider != null && hit.collider.CompareTag("Point"))
            {
                // Set the dragged point and flag as being dragged
                draggedPoint = hit.collider.gameObject;
                isDragging = true;
            }
        }

        // Check for right mouse button up
        if (Input.GetMouseButtonUp(1))
        {
            // Clear the dragged point and flag as not being dragged
            draggedPoint = null;
            isDragging = false;
        }

        // If a point is being dragged, update its position to the mouse position
        if (isDragging && draggedPoint != null)
        {
            Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mousePosition.z = 0f;
            draggedPoint.transform.position = mousePosition;
        }

        // If Space bar is pressed despawn the last point to be rendered
        if (Input.GetKeyDown(KeyCode.Space))
        {
            delaunayEdges.Clear();
            voronoiEdges.Clear();
            voronoiVertices.Clear();

            Destroy(spawnedPoints[spawnedPoints.Count - 1]);
            spawnedPoints.RemoveAt(spawnedPoints.Count - 1);

            Destroy(renderedLinesV[renderedLinesV.Count - 1]);
            renderedLinesV.RemoveAt(renderedLinesV.Count - 1);

            Destroy(renderedLinesD[renderedLinesD.Count - 1]);
            renderedLinesD.RemoveAt(renderedLinesD.Count - 1);
        }

        // If T key is pressed, toggle the toolbar
        if (Input.GetKeyUp(KeyCode.T))
            menu.SetActive(!menu.activeSelf);

        // If G key is pressed, generate random points on the screen
        if (Input.GetKeyUp(KeyCode.G))
            GenerateRandomPoints();

        if (Input.GetKeyUp(KeyCode.D))
        {
            if (!DL)
                DL = true;
            else
            {
                DL = false;
                while (renderedLinesD.Count != 0)
                {
                    GameObject line = renderedLinesD[renderedLinesD.Count - 1];

                    renderedLinesD.RemoveAt(renderedLinesD.Count - 1);
                    Destroy(line);
                }
            }
        }

        GenerateVD();
    }

    // Get all Voronoi edges and render them using line renderer
    void GenerateVD()
    {
        voronoiVertices.Clear();
        voronoiEdges.Clear();       

        List<Vector3> points = new List<Vector3>();
        for (int i = 0; i < spawnedPoints.Count; i++)
            points.Add(spawnedPoints[i].transform.position);

        delaunayEdges = Triangulate(points);
        while (renderedLinesV.Count != 0)
        {
            GameObject line = renderedLinesV[renderedLinesV.Count - 1];

            renderedLinesV.RemoveAt(renderedLinesV.Count - 1);
            Destroy(line);
        }

        for (int i = 0; i < voronoiEdges.Count; i++)
        {
            Vector3 a = voronoiEdges[i].start;
            Vector3 b = voronoiEdges[i].end;

            Debug.DrawLine(a, b, Color.black);

            GameObject line = new GameObject("Voronoi Edge");

            line.AddComponent<LineRenderer>();
            LineRenderer l = line.GetComponent<LineRenderer>();

            l.material = lineMaterialV;
            l.startWidth = 0.05f;
            l.endWidth = 0.05f;
            l.positionCount = 2;
            l.SetPosition(0, a);
            l.SetPosition(1, b);
            renderedLinesV.Add(line);
        }

        // Deal with trivial case
        if(spawnedPoints.Count == 2)
        {
            Vector3 point1 = spawnedPoints[0].transform.position;
            Vector3 point2 = spawnedPoints[1].transform.position;

            Vector3 midpoint = (point1 + point2) / 2f;
            Vector3 direction = (point2 - point1).normalized;
            Vector3 perpendicular = new Vector3(-direction.y * 20, direction.x * 20, 0f);

            Vector3[] vectors = { (midpoint - perpendicular), (midpoint + perpendicular) };

            LineSegment bisector = new LineSegment(vectors[0], vectors[1]);
            GameObject line = new GameObject("Voronoi Edge");

            line.AddComponent<LineRenderer>();
            LineRenderer l = line.GetComponent<LineRenderer>();

            l.material = lineMaterialV;
            l.startWidth = 0.05f;
            l.endWidth = 0.05f;
            l.positionCount = 2;
            l.SetPosition(0, bisector.start);
            l.SetPosition(1, bisector.end);
            renderedLinesV.Add(line);
        }
    }

    // Get all Delaunay edges and render them using line renderer
    void GenerateDL()
    {
        List<Vector3> points = new List<Vector3>();
        for (int i = 0; i < spawnedPoints.Count; i++)
            points.Add(spawnedPoints[i].transform.position);

        delaunayEdges = Triangulate(points);

        if(points.Count == 2) 
        {
            delaunayEdges.Add(delaunayEdges[0]);
            delaunayEdges.Add(delaunayEdges[1]);
        }

        while (renderedLinesD.Count != 0)
        {
            GameObject line = renderedLinesD[renderedLinesD.Count - 1];

            renderedLinesD.RemoveAt(renderedLinesD.Count - 1);
            Destroy(line);
        }

        for (int i = 0; i < delaunayEdges.Count; i++)
        {
            Vector3 a = delaunayEdges[i].A;
            Vector3 b = delaunayEdges[i].B;
            Vector3 c = delaunayEdges[i].C;

            Debug.DrawLine(a, b, Color.black);
            Debug.DrawLine(b, c, Color.black);
            Debug.DrawLine(c, a, Color.black);

            GameObject line = new GameObject("Delaunay Edge");

            line.AddComponent<LineRenderer>();
            LineRenderer l = line.GetComponent<LineRenderer>();

            l.material = lineMaterialD;
            l.startWidth = 0.03f;
            l.endWidth = 0.03f;
            l.positionCount = 4;
            l.SetPosition(0, a);
            l.SetPosition(1, b);
            l.SetPosition(2, c);
            l.SetPosition(3, a);
            renderedLinesD.Add(line);
        }
    }

    // Perform Delaunay Triangulation using the Bowyer-Watson algorithm,
    // and generate Voronoi Diagram using "Reverse Dual Algorithm"
    List<Triangle> Triangulate(List<Vector3> points)
    {
        //Create a super triangle containing all input points
        Vector3 superTriangleA = new Vector3(-5000, -5000, 0);
        Vector3 superTriangleB = new Vector3(5000, -5000, 0);
        Vector3 superTriangleC = new Vector3(0, 5000, 0);

        List<Triangle> triangles = new List<Triangle> { new Triangle(superTriangleA, superTriangleB, superTriangleC) };

        // Iterate through each point and update the triangulation
        foreach (Vector3 point in points)
        {
            List<Triangle> badTriangles = new List<Triangle>();

            // Find all triangles with circumcircles containing the current point
            foreach (Triangle triangle in triangles)
            {
                if (triangle.CircumcircleContains(point))
                {
                    badTriangles.Add(triangle);
                }
            }

            List<Vector3> polygon = new List<Vector3>();

            // Create a polygon from the edges of bad triangles not shared with other bad triangles
            foreach (Triangle triangle in badTriangles)
            {
                Vector3[] edges = new Vector3[] { triangle.A, triangle.B, triangle.B, triangle.C, triangle.C, triangle.A };

                for (int i = 0; i < edges.Length; i += 2)
                {
                    Vector3 edgeStart = edges[i];
                    Vector3 edgeEnd = edges[i + 1];

                    bool shared = false;

                    foreach (Triangle otherTriangle in badTriangles)
                    {
                        if (otherTriangle != triangle && otherTriangle.ContainsVertex(edgeStart) && otherTriangle.ContainsVertex(edgeEnd))
                        {
                            shared = true;
                            break;
                        }
                    }

                    // If the edge is not shared, add it to the polygon
                    if (!shared)
                    {
                        polygon.Add(edgeStart);
                        polygon.Add(edgeEnd);
                    }
                }
            }

            // Remove all bad triangles
            triangles.RemoveAll(triangle => badTriangles.Contains(triangle));

            // Add new triangles to the triangulation
            for (int i = 0; i < polygon.Count; i += 2)
            {
                Vector3 edgeStart = polygon[i];
                Vector3 edgeEnd = polygon[i + 1];

                Triangle newTriangle = new Triangle(point, edgeStart, edgeEnd);
                triangles.Add(newTriangle);
            }
        }

        // Remove triangles that share vertices with the super triangle
        triangles.RemoveAll(triangle => triangle.ContainsVertex(superTriangleA) || triangle.ContainsVertex(superTriangleB) || triangle.ContainsVertex(superTriangleC));

        // Calculate Voronoi edges using "Reverse Dual Algorithm"
        foreach (Triangle triangle in triangles)
        {
            foreach (Triangle otherTriangle in triangles)
            {
                // Check if the triangles share an edge
                if (triangle != otherTriangle && triangle.SharedEdge(otherTriangle))
                {
                    // Add the edge connecting the circumcenters of the two triangles
                    voronoiEdges.Add(new LineSegment(triangle.Circumcenter(), otherTriangle.Circumcenter()));
                }
            }
        }

        // Uses my Grahmscan implementation class
        voronoiVertices = ConvexHull.GrahamScan(points); 
        for (int i = 1; i < voronoiVertices.Count; i++)
        {
            LineSegment seg;

            foreach(Triangle triangle in triangles)
            {
                Vector3 A = triangle.A;
                Vector3 B = triangle.B;
                Vector3 C = triangle.C;

                if ((voronoiVertices[i - 1] == A && voronoiVertices[i] == B) || (voronoiVertices[i - 1] == B && voronoiVertices[i] == A))
                {
                    Vector3 center = triangle.Circumcenter();
                    seg = CalculateBisector(voronoiVertices[i - 1], voronoiVertices[i], center);

                    seg.start = center;
                    voronoiEdges.Add(seg);
                }
                else if ((voronoiVertices[i - 1] == B && voronoiVertices[i] == C) || (voronoiVertices[i - 1] == C && voronoiVertices[i] == B))
                {
                    Vector3 center = triangle.Circumcenter();
                    seg = CalculateBisector(voronoiVertices[i - 1], voronoiVertices[i], center);

                    seg.start = center;
                    voronoiEdges.Add(seg);
                }
                else if ((voronoiVertices[i - 1] == A && voronoiVertices[i] == C) || (voronoiVertices[i - 1] == C && voronoiVertices[i] == A))
                {
                    Vector3 center = triangle.Circumcenter();
                    seg = CalculateBisector(voronoiVertices[i - 1], voronoiVertices[i], center);

                    seg.start = center;
                    voronoiEdges.Add(seg);
                }

                if ((voronoiVertices[0] == A && voronoiVertices[voronoiVertices.Count - 1] == B) || (voronoiVertices[0] == B && voronoiVertices[voronoiVertices.Count - 1] == A))
                {
                    Vector3 center = triangle.Circumcenter();
                    seg = CalculateBisector(voronoiVertices[voronoiVertices.Count - 1], voronoiVertices[0], center);

                    seg.start = center;
                    voronoiEdges.Add(seg);
                    continue;
                }
                else if ((voronoiVertices[0] == B && voronoiVertices[voronoiVertices.Count - 1] == C) || (voronoiVertices[0] == C && voronoiVertices[voronoiVertices.Count - 1] == B))
                {
                    Vector3 center = triangle.Circumcenter();
                    seg = CalculateBisector(voronoiVertices[voronoiVertices.Count - 1], voronoiVertices[0], center);

                    seg.start = center;
                    voronoiEdges.Add(seg);
                    continue;
                }
                else if ((voronoiVertices[0] == A && voronoiVertices[voronoiVertices.Count - 1] == C) || (voronoiVertices[0] == C && voronoiVertices[voronoiVertices.Count - 1] == A))
                {
                    Vector3 center = triangle.Circumcenter();
                    seg = CalculateBisector(voronoiVertices[voronoiVertices.Count - 1], voronoiVertices[0], center);

                    seg.start = center;
                    voronoiEdges.Add(seg);
                    continue;
                }
            }
        }

        for (int i = 0; i < voronoiEdges.Count; i++)
        {
            // Draws the voronoi diagram in the debug window and not the game screen
            Debug.DrawLine(voronoiEdges[i].start, voronoiEdges[i].end, Color.green);
        }

        return triangles;
    }
    LineSegment CalculateBisector(Vector3 point1, Vector3 point2, Vector3 circumcenter)
    {
        // Calculate midpoint of the line segment between point1 and point2
        Vector3 midpoint = (point1 + point2) / 2f;

        // Calculate direction vector of the line segment between point1 and point2
        Vector3 direction = (point2 - point1).normalized;

        // Calculate perpendicular direction by swapping x and y components and negating one of them
        Vector3 perpendicular = new Vector3(-direction.y * 20, direction.x * 20, 0f);

        Vector3[] vectors = { (midpoint - perpendicular), (midpoint + perpendicular) };

        LineSegment bisector = new LineSegment(circumcenter, (circumcenter + perpendicular));

        return (bisector);
    }
    void GenerateRandomPoints()
    {
        for (int i = 0; i < numPoints; i++)
        {
            // Generate random coordinates within the game window boundary
            float randomX = Random.Range(-boundaryX, boundaryX);
            float randomY = Random.Range(-boundaryY, boundaryY);

            // Instantiate the point object at the generated coordinates
            Vector3 pointPosition = new Vector3(randomX, randomY, 0f);
            GameObject point = Instantiate(pointPrefab, pointPosition, Quaternion.identity);

            // Set the point object as a child of the script's game object for organization
            point.transform.parent = transform;

            // Add the spawned point to the list of spawned points
            spawnedPoints.Add(point);
        }
    }
}

// Class to store line segment structures
public class LineSegment
{
    public Vector3 start;
    public Vector3 end;

    public LineSegment(Vector3 start, Vector3 end)
    {
        this.start = start;
        this.end = end;
    }
}

// Class to store the vertices of a triangle
public class Triangle
{
    public Vector3 A, B, C;

    public Triangle(Vector3 a, Vector3 b, Vector3 c)
    {
        A = a;
        B = b;
        C = c;
    }

    // Check if two triangles share edges
    public bool SharedEdge(Triangle otherTriangle)
    {
        int sharedVertices = 0;
        if (ContainsVertex(otherTriangle.A)) sharedVertices++;
        if (ContainsVertex(otherTriangle.B)) sharedVertices++;
        if (ContainsVertex(otherTriangle.C)) sharedVertices++;

        return sharedVertices == 2;
    }

    // Check if the triangle contains a specific vertex
    public bool ContainsVertex(Vector3 vertex)
    {
        return A == vertex || B == vertex || C == vertex;
    }

    // Check if the circumcircle of the triangle contains a specific point
    public bool CircumcircleContains(Vector3 point)
    {
        float ab = A.sqrMagnitude;
        float cd = B.sqrMagnitude;
        float ef = C.sqrMagnitude;

        float circumX = (ab * (C.y - B.y) + cd * (A.y - C.y) + ef * (B.y - A.y)) / (2 * (A.x * (C.y - B.y) + B.x * (A.y - C.y) + C.x * (B.y - A.y)));
        float circumY = (ab * (C.x - B.x) + cd * (A.x - C.x) + ef * (B.x - A.x)) / (2 * (A.y * (C.x - B.x) + B.y * (A.x - C.x) + C.y * (B.x - A.x)));
        Vector3 circumCenter = new Vector3(circumX, circumY, 0);
        float circumRadius = (A - circumCenter).magnitude;

        return (point - circumCenter).sqrMagnitude <= circumRadius * circumRadius;
    }

    // Calculate the circumcenter of a triangle
    public Vector3 Circumcenter()
    {
        float ab = A.sqrMagnitude;
        float cd = B.sqrMagnitude;
        float ef = C.sqrMagnitude;

        float circumX = (ab * (C.y - B.y) + cd * (A.y - C.y) + ef * (B.y - A.y)) / (2 * (A.x * (C.y - B.y) + B.x * (A.y - C.y) + C.x * (B.y - A.y)));
        float circumY = (ab * (C.x - B.x) + cd * (A.x - C.x) + ef * (B.x - A.x)) / (2 * (A.y * (C.x - B.x) + B.y * (A.x - C.x) + C.y * (B.x - A.x)));
        Vector3 circumCenter = new Vector3(circumX, circumY, 0);

        return circumCenter;
    }
}