using System;
using System.Collections.Generic;
using UnityEngine;

public class ConvexHull
{
    public static List<Vector3> GrahamScan(List<Vector3> points)
    {
        Vector3 anchor = FindLowestYPoint(points);
        List<Vector3> sortedPoints = new List<Vector3>(points);

        // Sort the points by the angle they make with the anchor point
        sortedPoints.Sort((a, b) =>
        {
            float angleA = Mathf.Atan2(a.y - anchor.y, a.x - anchor.x);
            float angleB = Mathf.Atan2(b.y - anchor.y, b.x - anchor.x);

            if (angleA < angleB)
            {
                return -1;
            }
            else if (angleA > angleB)
            {
                return 1;
            }
            else
            {
                // If the points have the same angle, order them by distance to the anchor
                float distA = (a - anchor).sqrMagnitude;
                float distB = (b - anchor).sqrMagnitude;

                return distA.CompareTo(distB);
            }
        });

        Stack<Vector3> hull = new Stack<Vector3>();
        hull.Push(sortedPoints[0]);
        hull.Push(sortedPoints[1]);

        for (int i = 2; i < sortedPoints.Count; i++)
        {
            Vector3 top = hull.Pop();
            Vector3 nextToTop = hull.Peek();

            while (IsCounterClockwiseTurn(nextToTop, top, sortedPoints[i]) <= 0)
            {
                top = hull.Pop();
                nextToTop = hull.Peek();
            }

            hull.Push(top);
            hull.Push(sortedPoints[i]);
        }

        return new List<Vector3>(hull);
    }

    private static Vector3 FindLowestYPoint(List<Vector3> points)
    {
        Vector3 lowest = points[0];

        for (int i = 1; i < points.Count; i++)
        {
            if (points[i].y < lowest.y || (points[i].y == lowest.y && points[i].x < lowest.x))
            {
                lowest = points[i];
            }
        }

        return lowest;
    }

    private static float IsCounterClockwiseTurn(Vector3 a, Vector3 b, Vector3 c)
    {
        return (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x);
    }
}