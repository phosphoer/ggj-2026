// The MIT License (MIT)
// Copyright (c) 2024 David Evans @festivevector
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

// This is based off the Mathfx from Unity Wiki but I added my own stuff that I find useful and removed stuff that was redundant with Unity and added some docs

using UnityEngine;
using System.Collections.Generic;

using Unity.Mathematics;

public static class Mathfx
{
  // Interpolation functions 
  public static float Hermite(float start, float end, float value)
  {
    return Mathf.Lerp(start, end, value * value * (3.0f - 2.0f * value));
  }

  public static float Sinerp(float start, float end, float value)
  {
    return Mathf.Lerp(start, end, Mathf.Sin(value * Mathf.PI * 0.5f));
  }

  public static float SmoothSin(float start, float end, float value)
  {
    return Mathf.Lerp(start, end, Mathf.Sin(value * 2 * Mathf.PI + Mathf.PI * 0.5f));
  }

  public static float Coserp(float start, float end, float value)
  {
    return Mathf.Lerp(start, end, 1.0f - Mathf.Cos(value * Mathf.PI * 0.5f));
  }

  public static float Berp(float start, float end, float value)
  {
    value = Mathf.Clamp01(value);
    value = (Mathf.Sin(value * Mathf.PI * (0.2f + 2.5f * value * value * value)) * Mathf.Pow(1f - value, 2.2f) + value) * (1f + (1.2f * (1f - value)));
    return start + (end - start) * value;
  }

  public static float SmoothStep(float min, float max, float x)
  {
    x = Mathf.Clamp(x, min, max);
    float v1 = (x - min) / (max - min);
    float v2 = (x - min) / (max - min);
    return -2 * v1 * v1 * v1 + 3 * v2 * v2;
  }

  public static float Bounce(float x)
  {
    return Mathf.Abs(Mathf.Sin(6.28f * (x + 1f) * (x + 1f)) * (1f - x));
  }

  public static float WrappedDelta01(float a, float b)
  {
    float delta = Mathf.Abs(a - b);
    if (delta > 0.5f)
      return 1 - delta;

    return delta;
  }

  // Wrap a value between min and max, handy for incrementing a value with wrapping
  // NOTE that this does not wrap around modulo style
  // Any input value above max will return min, and vice versa
  public static int Wrap(int value, int minInclusive, int maxInclusive)
  {
    if (value > maxInclusive)
      return minInclusive;

    if (value < minInclusive)
      return maxInclusive;

    return value;
  }

  /*
   * CLerp - Circular Lerp - is like lerp but handles the wraparound from 0 to 360.
   * This is useful when interpolating eulerAngles and the object
   * crosses the 0/360 boundary.  The standard Lerp function causes the object
   * to rotate in the wrong direction and looks stupid. Clerp fixes that.
   */
  public static float Clerp(float start, float end, float value)
  {
    float min = 0.0f;
    float max = 360.0f;
    float half = Mathf.Abs((max - min) / 2.0f); //half the distance between min and max
    float retval = 0.0f;
    float diff = 0.0f;

    if ((end - start) < -half)
    {
      diff = ((max - start) + end) * value;
      retval = start + diff;
    }
    else if ((end - start) > half)
    {
      diff = -((max - end) + start) * value;
      retval = start + diff;
    }
    else retval = start + (end - start) * value;

    return retval;
  }

  // By Inigo Quilez, under MIT license
  // https://www.shadertoy.com/view/ttcyRS
  // Translated to c# by @festivevector
  public static Color ColorLerpOKLab(Color colorA, Color colorB, float t)
  {
    Color linearA = colorA.linear;
    Color linearB = colorB.linear;
    float3 a = new(linearA.r, linearA.g, linearA.b);
    float3 b = new(linearB.r, linearB.g, linearB.b);

    float3x3 kCONEtoLMS = new(
         0.4121656120f, 0.5362752080f, 0.0514575653f,
         0.2118591070f, 0.6807189584f, 0.1074065790f,
         0.0883097947f, 0.2818474174f, 0.6302613616f);

    float3x3 kLMStoCONE = new(
         4.0767245293f, -3.3072168827f, 0.2307590544f,
        -1.2681437731f, 2.6093323231f, -0.3411344290f,
        -0.0041119885f, -0.7034763098f, 1.7068625689f);

    // rgb to cone (arg of pow can't be negative)
    float3 lms1 = math.pow(math.mul(kCONEtoLMS, a), new float3(1.0 / 3.0));
    float3 lms2 = math.pow(math.mul(kCONEtoLMS, b), new float3(1.0 / 3.0));
    // lerp
    float3 lms = math.lerp(lms1, lms2, t);
    // gain in the middle (no oklab anymore, but looks better?)
    lms *= 1.0f + 0.2f * t * (1.0f - t);

    // cone to rgb
    float3 blended = math.mul(kLMStoCONE, lms * lms * lms);
    return new Color(blended.x, blended.y, blended.z, Mathf.Lerp(linearA.a, linearB.a, t)).gamma;
  }

  // Dampening functions, stateless-ly and frame independently interpolate towards a value
  // Smoothing in range from 0 (will instantly reach target) to 1 (never reaches target)
  // I like to use around 0.5
  // https://www.rorydriscoll.com/2016/03/07/frame-rate-independent-damping-using-lerp/
  public static float Damp(float source, float target, float smoothing, float dt, float snapEpsilon = 0.01f)
  {
    float val = Mathf.Lerp(source, target, 1 - Mathf.Pow(smoothing, dt));
    if (Mathf.Abs(val - target) < snapEpsilon)
      val = target;

    return val;
  }

  public static Vector4 Damp(Vector4 source, Vector4 target, float smoothing, float dt)
  {
    return Vector4.Lerp(source, target, 1 - Mathf.Pow(smoothing, dt));
  }

  public static Vector3 Damp(Vector3 source, Vector3 target, float smoothing, float dt)
  {
    return Vector3.Lerp(source, target, 1 - Mathf.Pow(smoothing, dt));
  }

  public static Vector2 Damp(Vector2 source, Vector2 target, float smoothing, float dt)
  {
    return Vector2.Lerp(source, target, 1 - Mathf.Pow(smoothing, dt));
  }

  public static Color Damp(Color source, Color target, float smoothing, float dt)
  {
    return Color.Lerp(source, target, 1 - Mathf.Pow(smoothing, dt));
  }

  public static Quaternion Damp(Quaternion source, Quaternion target, float smoothing, float dt)
  {
    return Quaternion.Lerp(source, target, 1 - Mathf.Pow(smoothing, dt));
  }

  // test if a Vector3 is close to another Vector3 (due to floating point inprecision)
  // compares the square of the distance to the square of the range as this 
  // avoids calculating a square root which is much slower than squaring the range
  public static bool Approx(Vector3 val, Vector3 about, float range)
  {
    return ((val - about).sqrMagnitude < range * range);
  }

  public enum Axis
  {
    X = 0,
    Y = 1,
    Z = 2,
  }

  public enum Channel
  {
    R = 0,
    G = 1,
    B = 2,
    A = 3,
  }

  private static Vector3[] _axisVectors = new Vector3[]
  {
    Vector3.right,
    Vector3.up,
    Vector3.forward
  };

  public static Vector3 AxisToVector(Axis axis)
  {
    return _axisVectors[(int)axis];
  }

  public static Axis VectorToAxis(Vector3 basisVector)
  {
    Vector3 minDeltas = Vector3.positiveInfinity;
    Vector3 basisVectorAbs = basisVector.AsAbsoluteValues();
    int minIndex = 0;
    for (int i = 0; i < _axisVectors.Length; ++i)
    {
      Vector3 axisVector = _axisVectors[i];
      Vector3 delta = basisVectorAbs - axisVector;
      if (delta.x <= minDeltas.x && delta.y <= minDeltas.y && delta.z <= minDeltas.z)
      {
        minDeltas = delta;
        minIndex = i;
      }
    }

    return (Axis)minIndex;
  }

  // Project a point onto a plane, not necessarily at origin
  // Vector3.ProjectOnPlane assumes origin for the plane
  public static Vector3 ProjectPointOnPlane(Vector3 point, Plane plane)
  {
    return Vector3.ProjectOnPlane(point, plane.normal) - plane.normal * plane.distance;
  }

  // Closest distance between two lines
  public static float DistanceBetweenVectors(Vector3 origin1, Vector3 direction1, Vector3 origin2, Vector3 direction2)
  {
    // Compute dot products
    Vector3 delta = origin1 - origin2;
    float a = Vector3.Dot(direction1, direction1);
    float b = Vector3.Dot(direction1, direction2);
    float c = Vector3.Dot(direction2, direction2);
    float d = Vector3.Dot(direction1, delta);
    float e = Vector3.Dot(direction2, delta);

    float denominator = a * c - b * b;
    if (Mathf.Abs(denominator) < Mathf.Epsilon)
    {
      Vector3 crossProduct = Vector3.Cross(direction1, delta);
      return crossProduct.magnitude / direction1.magnitude;
    }

    // Compute the closest points on the two lines
    float t1 = (b * e - c * d) / denominator;
    float t2 = (a * e - b * d) / denominator;
    Vector3 closestPoint1 = origin1 + t1 * direction1;
    Vector3 closestPoint2 = origin2 + t2 * direction2;

    // Compute the vector between these two closest points and its length
    Vector3 closestPointDelta = closestPoint1 - closestPoint2;
    return closestPointDelta.magnitude;
  }

  // Nearest point on an infinite line to a point
  public static Vector3 NearestPointLine(Vector3 lineStart, Vector3 lineEnd, Vector3 point)
  {
    Vector3 lineDirection = Vector3.Normalize(lineEnd - lineStart);
    float closestPoint = Vector3.Dot(point - lineStart, lineDirection) / Vector3.Dot(lineDirection, lineDirection);
    return lineStart + (closestPoint * lineDirection);
  }

  // Nearest point on an infinite line to a point
  public static Vector3 NearestPointRay(Vector3 rayOrigin, Vector3 rayDirection, Vector3 point)
  {
    Vector3 lineDirection = Vector3.Normalize(rayDirection);
    float closestPoint = Vector3.Dot(point - rayOrigin, lineDirection) / Vector3.Dot(lineDirection, lineDirection);
    return rayOrigin + (closestPoint * lineDirection);
  }

  // Nearest point on a line, clamped to the endpoints of the line segment
  public static Vector3 NearestPointSegment(Vector3 lineStart, Vector3 lineEnd, Vector3 point)
  {
    Vector3 fullDirection = lineEnd - lineStart;
    Vector3 lineDirection = Vector3.Normalize(fullDirection);
    float closestPoint = Vector3.Dot((point - lineStart), lineDirection) / Vector3.Dot(lineDirection, lineDirection);
    return lineStart + (Mathf.Clamp(closestPoint, 0.0f, Vector3.Magnitude(fullDirection)) * lineDirection);
  }

  public static int NearestPointPath(IReadOnlyList<Vector3> path, Vector3 point, out Vector3 nearestPoint)
  {
    float minDist = Mathf.Infinity;
    int nearestIndex = 0;
    nearestPoint = point;
    for (int i = 0; i < path.Count - 1; ++i)
    {
      Vector3 nearestPointSegment = NearestPointSegment(path[i], path[i + 1], point);
      float dist = Vector3.Distance(point, nearestPointSegment);
      if (dist < minDist)
      {
        minDist = dist;
        nearestPoint = nearestPointSegment;
        nearestIndex = i;
      }
    }

    return nearestIndex;
  }

  public static bool IntersectRayWithSphere(Vector3 rayOrigin, Vector3 rayDir, Vector3 sphereCenter, float sphereRadius, out float intersectT)
  {
    intersectT = 0;
    Vector3 rayToSphereCenter = sphereCenter - rayOrigin;
    float dotRayToSphere = Vector3.Dot(rayToSphereCenter, rayDir);
    float d2 = Vector3.Dot(rayToSphereCenter, rayToSphereCenter) - dotRayToSphere * dotRayToSphere;
    if (d2 > sphereRadius * sphereRadius)
      return false;

    float thc = Mathf.Sqrt(sphereRadius * sphereRadius - d2);
    float t0 = dotRayToSphere - thc;
    float t1 = dotRayToSphere + thc;
    if (t0 > t1)
    {
      float swap = t0;
      t0 = t1;
      t1 = swap;
    }

    if (t0 < 0)
    {
      t0 = t1;
      if (t0 < 0)
        return false;
    }

    intersectT = t0;
    return true;
  }

  public static Vector3 SampleFibonacciSphere(int index, int sampleCount)
  {
    const float kSqrt5 = 2.2360679775f;
    const float kPhi = Mathf.PI * (kSqrt5 - 1);

    float y = 1 - index / (float)(sampleCount - 1) * 2;
    float radius = Mathf.Sqrt(1 - y * y);
    float theta = kPhi * index;
    float x = Mathf.Cos(theta) * radius;
    float z = Mathf.Sin(theta) * radius;
    Vector3 point = new(x, y, z);
    return point;
  }

  public static void FillFibonacciSphere(int sampleCount, List<Vector3> outputPoints)
  {
    for (int i = 0; i < sampleCount; ++i)
    {
      outputPoints.Add(SampleFibonacciSphere(i, sampleCount));
    }
  }

  public static Vector3 ClampToRadius(Vector3 point, Vector3 center, float radius)
  {
    Vector3 offset = point - center;
    offset = Vector3.ClampMagnitude(offset, radius);
    point = center + offset;
    return point;
  }

  // 
  // Canvas / Rect Transform helpers
  //
  public static Vector3 ViewportToCanvasPosition(RectTransform canvas, Vector3 viewportPos)
  {
    viewportPos.x *= canvas.rect.size.x;
    viewportPos.y *= canvas.rect.size.y;
    viewportPos.x -= canvas.rect.size.x * canvas.pivot.x;
    viewportPos.y -= canvas.rect.size.y * canvas.pivot.y;
    return viewportPos;
  }

  public static Vector3 WorldToCanvasPosition(RectTransform canvas, Camera camera, Vector3 worldPos, bool allowOffscreen = false)
  {
    Vector3 pos = camera.WorldToViewportPoint(worldPos);
    if (pos.z < 0 && !allowOffscreen)
    {
      pos.y = 0;
      pos.x = 1 - pos.x;
    }

    pos = ViewportToCanvasPosition(canvas, pos);
    return pos;
  }

  public static Vector3 CanvasToWorldPosition(RectTransform canvas, Camera camera, Vector2 canvasLocalPos)
  {
    Vector2 viewportPoint = canvasLocalPos + canvas.rect.size * canvas.pivot;
    viewportPoint /= canvas.rect.size;
    Vector3 worldPos = camera.ViewportToWorldPoint(viewportPoint, Camera.MonoOrStereoscopicEye.Mono);
    return worldPos;
  }

  public static Vector3[] GetRectTransformCorners(RectTransform transform)
  {
    transform.GetWorldCorners(_rectWorldCorners);
    return _rectWorldCorners;
  }

  public static Vector3 GetRectTransformWorldSize(RectTransform transform)
  {
    transform.GetWorldCorners(_rectWorldCorners);
    return _rectWorldCorners[2] - _rectWorldCorners[0];
  }

  private static Vector3[] _rectWorldCorners = new Vector3[4];

  //
  // Physics helpers
  // 
  public static bool RaycastIgnoringTransform(Vector3 origin,
    Vector3 direction,
    out RaycastHit hitInfo,
    float maxDist,
    LayerMask layerMask,
    Transform ignoreTransform,
    QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.Ignore)
  {
    int tries = 100;
    bool isDone = false;
    while (!isDone && Physics.Raycast(origin, direction, out RaycastHit hit, maxDist, layerMask, queryTriggerInteraction) && tries-- > 0)
    {
      hitInfo = hit;
      origin = hitInfo.point + direction * 0.1f;
      isDone = ignoreTransform == null || (hitInfo.transform != ignoreTransform && !hitInfo.transform.IsChildOf(ignoreTransform));
      if (isDone)
        return true;
    }

    hitInfo = default;
    return false;
  }

  public static bool SphereCastIgnoringTransform(Vector3 origin, float radius, Vector3 direction, out RaycastHit hitInfo, float maxDist, LayerMask layerMask, Transform ignoreTransform)
  {
    int tries = 100;
    bool isDone = false;
    while (!isDone && Physics.SphereCast(origin, radius, direction, out RaycastHit hit, maxDist, layerMask) && tries-- > 0)
    {
      hitInfo = hit;
      origin += direction * maxDist * 0.01f;
      isDone = ignoreTransform == null || (hitInfo.transform != ignoreTransform && !hitInfo.transform.IsChildOf(ignoreTransform));
      if (isDone)
        return true;
    }

    hitInfo = default;
    return false;
  }

  //
  // Misc math
  //
  public static float CeilNormal(float val)
  {
    if (val > 0)
      return 1.0f;
    else if (val < 0)
      return -1.0f;
    return 0.0f;
  }

  public static int GetRadialIndex(float signedAngle, int arcCount)
  {
    float angleStep = 360f / arcCount;
    float angle360 = signedAngle >= 0 ? signedAngle : 360 + signedAngle;

    angle360 += angleStep / 2;
    if (angle360 > 360)
      angle360 -= 360;

    int radialIndex = Mathf.FloorToInt((angle360 / 360.0f) * arcCount);
    radialIndex = Mathf.Clamp(radialIndex, 0, arcCount - 1);
    return radialIndex;
  }

  // https://answers.unity.com/questions/283192/how-to-convert-decibel-number-to-audio-source-volu.html
  public static float LinearToDecibel(float linear)
  {
    float dB;

    if (linear != 0)
      dB = 20.0f * Mathf.Log10(linear);
    else
      dB = -144.0f;

    return dB;
  }

  public static float DecibelToLinear(float dB)
  {
    float linear = Mathf.Pow(10.0f, dB / 20.0f);

    return linear;
  }

  // Inverted bounds, useful if you want to accumulate bounds without including origin
  public static Bounds NegativeBounds()
  {
    Bounds negativeBounds = new Bounds();
    negativeBounds.min = Vector3.positiveInfinity;
    negativeBounds.max = Vector3.negativeInfinity;
    return negativeBounds;
  }

  public static Vector3 RandomInBounds(Bounds bounds, Quaternion rotation)
  {
    Vector3 pos = Vector3.zero;
    pos += Vector3.right * UnityEngine.Random.Range(-bounds.size.x, bounds.size.x);
    pos += Vector3.up * UnityEngine.Random.Range(-bounds.size.y, bounds.size.y);
    pos += Vector3.forward * UnityEngine.Random.Range(-bounds.size.z, bounds.size.z);
    pos = rotation * pos + bounds.center;
    return pos;
  }

  // Get a random index from indexCount items, weighted by the given weight function for each index
  // useful for things like loot tables
  public static int GetWeightedRandomIndex(int indexCount, System.Func<int, float> weightFunction)
  {
    float totalWeight = 0;
    for (int i = 0; i < indexCount; ++i)
    {
      totalWeight += weightFunction(i);
    }

    float rnd = UnityEngine.Random.Range(0, totalWeight);
    for (int i = 0; i < indexCount; ++i)
    {
      float weight = weightFunction(i);
      if (rnd < weight)
        return i;

      rnd -= weight;
    }

    return -1;
  }

  public static float GetRectRadius(Vector2 vec, Rect rect)
  {
    float vecAngle = Mathf.Deg2Rad * Vector2.Angle(vec.WithY(0), vec);
    float radiusVertical = (rect.height / 2) / Mathf.Sin(vecAngle);
    float radiusHorizontal = (rect.width / 2) / Mathf.Cos(vecAngle);
    return Mathf.Min(radiusVertical, radiusHorizontal);
  }

  // 2D Convex hull of a list of points on the XZ plane
  public static void ConvexHull(IReadOnlyList<Vector3> points, List<Vector3> hullPoints, int maxPoints = 64)
  {
    hullPoints.Clear();
    if (points.Count < 3)
    {
      return;
    }

    var minPoint = points[0];
    foreach (var p in points)
    {
      if (p.x < minPoint.x)
        minPoint = p;
      else if (Mathf.Abs(p.x - minPoint.x) < Mathf.Epsilon && p.z < minPoint.z)
        minPoint = p;
    }

    var nextHullPoint = minPoint;
    hullPoints.Add(nextHullPoint);
    do
    {
      nextHullPoint = ConvexNextPoint(points, nextHullPoint);
      if (Vector3.Distance(nextHullPoint, hullPoints[0]) > Mathf.Epsilon)
        hullPoints.Add(nextHullPoint);
    } while (Vector3.Distance(nextHullPoint, hullPoints[0]) > Mathf.Epsilon && hullPoints.Count < maxPoints);
  }

  private static int ConvexTurn(Vector3 p, Vector3 q, Vector3 r)
  {
    var val = (q.x - p.x) * (r.z - p.z) - (r.x - p.x) * (q.z - p.z);
    if (val < 0)
      return -1;
    if (val == 0)
      return 0;
    return 1;
  }

  private static Vector3 ConvexNextPoint(IReadOnlyList<Vector3> points, Vector3 p)
  {
    var q = p;
    foreach (var r in points)
    {
      var t = ConvexTurn(p, q, r);
      if (t == -1 || t == 0 && ConvexDistance(p, r) > ConvexDistance(p, q))
        q = r;
    }

    return q;
  }

  private static float ConvexDistance(Vector3 p, Vector3 q)
  {
    return Vector3.SqrMagnitude(p - q);
  }
}

public static class BoundsExtensions
{
  public static Vector3 ClosestPointOnSurface(this Bounds bounds, Vector3 v)
  {
    if (bounds.Contains(v))
    {
      Vector3 boundsLocalPoint = v - bounds.center;
      Vector3 insideDelta = bounds.extents - boundsLocalPoint.AsAbsoluteValues();
      int minComponent = 0;
      float minComponentValue = insideDelta.x;
      // QAG.ConsoleDebug.Log($"Local point is {boundsLocalPoint}");
      // QAG.ConsoleDebug.Log($"Inside delta is {insideDelta}");
      for (int i = 0; i < 3; ++i)
      {
        if (insideDelta[i] < minComponentValue)
        {
          minComponentValue = insideDelta[i];
          minComponent = i;
        }
      }

      float localPointSide = Mathf.Sign(boundsLocalPoint[minComponent]);
      Vector3 planeNormal = Vector3.zero.WithComponent(minComponent, localPointSide);
      Vector3 planePos = Vector3.zero.WithComponent(minComponent, bounds.extents[minComponent] * localPointSide);
      planePos += bounds.center;

      return Mathfx.ProjectPointOnPlane(v, new Plane(planeNormal, planePos));
    }

    Vector3 closestPoint = bounds.ClosestPoint(v);
    return closestPoint;
  }

  public static Vector3 GetNormalizedCoords(this Bounds bounds, Vector3 worldPos)
  {
    Vector3 coords = worldPos - bounds.min;
    return new Vector3(coords.x / bounds.size.x, coords.y / bounds.size.y, coords.z / bounds.size.z);
  }

  public static Vector3 Clamp(this Bounds bounds, Vector3 v)
  {
    v = Vector3.Max(v, bounds.min);
    v = Vector3.Min(v, bounds.max);
    return v;
  }

  public static Vector3 IntersectRayWithCenter(this Bounds bounds, Vector3 fromPoint)
  {
    if (bounds.Contains(fromPoint))
      return fromPoint;

    Vector3 toCenter = bounds.center - fromPoint;
    float hitDistance = 0;
    bounds.IntersectRay(new Ray(fromPoint, toCenter), out hitDistance);
    return fromPoint + toCenter.normalized * hitDistance;
  }
}

public static class VectorExtensions
{
  // Get a copy of this vector with a given value for x/y/z
  // My primary use case is getting a forward vector whose y is 0 
  public static Vector2 WithX(this Vector2 v, float x)
  {
    v.x = x;
    return v;
  }

  public static Vector2 WithY(this Vector2 v, float y)
  {
    v.y = y;
    return v;
  }

  public static Vector2 NormalizedSafe(this Vector2 v)
  {
    if (v.sqrMagnitude == 0)
      return Vector2.zero;

    return v.normalized;
  }

  public static Vector3 NormalizedSafe(this Vector3 v)
  {
    if (v.sqrMagnitude == 0)
      return Vector3.zero;

    return v.normalized;
  }

  public static Vector2 WithMagnitude(this Vector2 v, float magnitude)
  {
    return v.NormalizedSafe() * magnitude;
  }

  public static Vector3 WithMagnitude(this Vector3 v, float magnitude)
  {
    return v.NormalizedSafe() * magnitude;
  }

  public static Vector3 OnXZPlane(this Vector2 v)
  {
    return new Vector3(v.x, 0, v.y);
  }

  public static Vector3 OnXYPlane(this Vector2 v)
  {
    return new Vector3(v.x, v.y, 0);
  }

  public static Vector3 WithX(this Vector3 v, float x)
  {
    v.x = x;
    return v;
  }

  public static Vector3 WithY(this Vector3 v, float y)
  {
    v.y = y;
    return v;
  }

  public static Vector3 WithZ(this Vector3 v, float z)
  {
    v.z = z;
    return v;
  }

  public static Vector4 WithW(this Vector3 v, float w)
  {
    return new Vector4(v.x, v.y, v.z, w);
  }

  public static Vector3 WithComponent(this Vector3 v, int i, float val)
  {
    v[i] = val;
    return v;
  }

  public static Vector3 AsAbsoluteValues(this Vector3 v)
  {
    v.x = Mathf.Abs(v.x);
    v.y = Mathf.Abs(v.y);
    v.z = Mathf.Abs(v.z);
    return v;
  }

  public static Vector2 XY(this Vector3 vector)
  {
    return new Vector2(vector.x, vector.y);
  }

  public static Vector2 XZ(this Vector3 vector)
  {
    return new Vector2(vector.x, vector.z);
  }

  public static float Component(this Vector3 vector, Mathfx.Axis axis)
  {
    return vector[(int)axis];
  }

  public static float Component(this Vector2 vector, Mathfx.Axis axis)
  {
    return vector[(int)axis];
  }

  public static float SquareDistance(this Vector3 v, Vector3 rhs)
  {
    return (rhs - v).sqrMagnitude;
  }
}

public static class ColorExtensions
{
  public static Color WithR(this Color c, float r)
  {
    c.r = r;
    return c;
  }

  public static Color WithG(this Color c, float g)
  {
    c.g = g;
    return c;
  }

  public static Color WithB(this Color c, float b)
  {
    c.b = b;
    return c;
  }

  public static Color WithA(this Color c, float a)
  {
    c.a = a;
    return c;
  }

  public static Color WithChannel(this Color c, Mathfx.Channel channel, float val)
  {
    c[(int)channel] = val;
    return c;
  }
}