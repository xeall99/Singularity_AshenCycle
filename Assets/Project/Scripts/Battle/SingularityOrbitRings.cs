using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public class SingularityOrbitRings : MaskableGraphic
{
    [Header("Ring Shape")]
    [SerializeField, Range(1, 5)] private int ringCount = 3;
    [SerializeField, Range(24, 160)] private int segmentsPerRing = 72;
    [SerializeField, Min(0.5f)] private float lineThickness = 2.5f;
    [SerializeField, Min(1f)] private float ringSpacing = 16f;
    [SerializeField, Min(0f)] private float outerPadding = 8f;
    [SerializeField, Range(0.15f, 1f)] private float ellipseHeight = 0.58f;
    [SerializeField, Range(0.25f, 0.98f)] private float arcCoverage = 0.78f;

    [Header("Ring Motion")]
    [SerializeField] private float baseRotationSpeed = 18f;
    [SerializeField] private float rotationSpeedDifference = 9f;
    [SerializeField] private float pulseSpeed = 2.4f;
    [SerializeField, Range(0f, 0.15f)] private float pulseAmount = 0.035f;

    [Header("Ring Appearance")]
    [SerializeField, Range(0f, 1f)] private float innerRingAlpha = 0.45f;

    private float animationTime;

    protected override void Awake()
    {
        base.Awake();
        raycastTarget = false;
    }

    private void Update()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        animationTime += Time.unscaledDeltaTime;
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vertexHelper)
    {
        vertexHelper.Clear();

        Rect drawingRect = rectTransform.rect;

        float largestRadiusX = Mathf.Max(
            0f,
            drawingRect.width * 0.5f - outerPadding
        );

        float largestRadiusY = Mathf.Max(
            0f,
            drawingRect.height * 0.5f - outerPadding
        );

        if (largestRadiusX <= lineThickness ||
            largestRadiusY <= lineThickness)
        {
            return;
        }

        int arcSegments = Mathf.Max(
            2,
            Mathf.RoundToInt(segmentsPerRing * arcCoverage)
        );

        float angleStep = Mathf.PI * 2f / segmentsPerRing;

        for (int ringIndex = 0; ringIndex < ringCount; ringIndex++)
        {
            float radiusX = largestRadiusX - ringIndex * ringSpacing;

            float radiusY = Mathf.Min(
                largestRadiusY - ringIndex * ringSpacing * ellipseHeight,
                radiusX * ellipseHeight
            );

            if (radiusX <= lineThickness || radiusY <= lineThickness)
            {
                continue;
            }

            float direction = ringIndex % 2 == 0 ? 1f : -1f;

            float rotationSpeed =
                baseRotationSpeed +
                ringIndex * rotationSpeedDifference;

            float rotationOffset =
                animationTime * rotationSpeed * direction * Mathf.Deg2Rad +
                ringIndex * 2.15f;

            float pulse = 1f +
                Mathf.Sin(
                    animationTime * pulseSpeed + ringIndex * 1.7f
                ) * pulseAmount;

            float currentRadiusX = radiusX * pulse;
            float currentRadiusY = radiusY * pulse;

            Color ringColour = color;

            float ringProgress = ringCount <= 1
                ? 0f
                : ringIndex / (float)(ringCount - 1);

            ringColour.a *= Mathf.Lerp(
                1f,
                innerRingAlpha,
                ringProgress
            );

            DrawArc(
                vertexHelper,
                currentRadiusX,
                currentRadiusY,
                rotationOffset,
                angleStep,
                arcSegments,
                ringColour
            );
        }
    }

    private void DrawArc(
        VertexHelper vertexHelper,
        float radiusX,
        float radiusY,
        float rotationOffset,
        float angleStep,
        int arcSegments,
        Color ringColour
    )
    {
        float halfThickness = lineThickness * 0.5f;

        for (int segmentIndex = 0;
             segmentIndex < arcSegments;
             segmentIndex++)
        {
            float firstAngle =
                rotationOffset + segmentIndex * angleStep;

            float secondAngle = firstAngle + angleStep;

            Vector2 firstPoint = GetEllipsePoint(
                firstAngle,
                radiusX,
                radiusY
            );

            Vector2 secondPoint = GetEllipsePoint(
                secondAngle,
                radiusX,
                radiusY
            );

            Vector2 firstNormal = GetEllipseNormal(
                firstAngle,
                radiusX,
                radiusY
            );

            Vector2 secondNormal = GetEllipseNormal(
                secondAngle,
                radiusX,
                radiusY
            );

            int firstVertexIndex = vertexHelper.currentVertCount;

            AddVertex(
                vertexHelper,
                firstPoint + firstNormal * halfThickness,
                ringColour
            );

            AddVertex(
                vertexHelper,
                firstPoint - firstNormal * halfThickness,
                ringColour
            );

            AddVertex(
                vertexHelper,
                secondPoint + secondNormal * halfThickness,
                ringColour
            );

            AddVertex(
                vertexHelper,
                secondPoint - secondNormal * halfThickness,
                ringColour
            );

            vertexHelper.AddTriangle(
                firstVertexIndex,
                firstVertexIndex + 1,
                firstVertexIndex + 2
            );

            vertexHelper.AddTriangle(
                firstVertexIndex + 2,
                firstVertexIndex + 1,
                firstVertexIndex + 3
            );
        }
    }

    private static Vector2 GetEllipsePoint(
        float angle,
        float radiusX,
        float radiusY
    )
    {
        return new Vector2(
            Mathf.Cos(angle) * radiusX,
            Mathf.Sin(angle) * radiusY
        );
    }

    private static Vector2 GetEllipseNormal(
        float angle,
        float radiusX,
        float radiusY
    )
    {
        Vector2 tangent = new Vector2(
            -radiusX * Mathf.Sin(angle),
            radiusY * Mathf.Cos(angle)
        ).normalized;

        return new Vector2(tangent.y, -tangent.x);
    }

    private static void AddVertex(
        VertexHelper vertexHelper,
        Vector2 position,
        Color colour
    )
    {
        UIVertex vertex = UIVertex.simpleVert;
        vertex.position = position;
        vertex.color = colour;
        vertexHelper.AddVert(vertex);
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();

        ringCount = Mathf.Clamp(ringCount, 1, 5);
        segmentsPerRing = Mathf.Clamp(segmentsPerRing, 24, 160);
        lineThickness = Mathf.Max(0.5f, lineThickness);
        ringSpacing = Mathf.Max(1f, ringSpacing);
        outerPadding = Mathf.Max(0f, outerPadding);

        SetVerticesDirty();
    }
#endif
}
