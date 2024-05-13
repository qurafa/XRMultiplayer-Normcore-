using com.perceptlab.armultiplayer;
using UnityEngine;

public static class AlignHelpers
{
    public static void moveDadToMakeChildMatchDestination(GameObject child, GameObject dad, Vector3 destination)
    {
        Vector3 diff = dad.transform.position - child.transform.position;
        dad.transform.position = destination + diff;
    }

    /// <summary>
    /// Assumption: child is under dad in the Hierarchy.
    /// Rotates dad around child object to make it face direction without translating the child object.
    /// The result of this operation is that the child's transform's direction collapsed to the x-z plane with match that of the direction.
    /// </summary>
    /// <param name="child"></param>
    /// <param name="dad"></param>
    /// <param name="direction"></param>
    public static void rotateDadtoMakeChildFaceDirection(GameObject child, GameObject dad, Vector3 direction)
    {
        RLogger.Log($"[AlignHelpers:rotateDadtoMakeChildFaceDirection] called, dad forward is {dad.transform.forward}");
        direction.y = 0; direction.Normalize(); RLogger.Log($"direction is: {direction}");
        Vector3 childFacing = child.transform.forward; childFacing.y = 0; childFacing.Normalize(); RLogger.Log($"child is facing: {childFacing}");
        float angle = Vector3.SignedAngle(childFacing, direction, Vector3.up); RLogger.Log($"rotation from child to direction is: {angle}");
        dad.transform.RotateAround(child.transform.position, Vector3.up, angle);
        RLogger.Log($"rotated dad");
        childFacing = child.transform.forward; childFacing.y = 0; childFacing.Normalize(); RLogger.Log($"dad forward is {dad.transform.forward}--child is facing: {childFacing}");
    }

    /// <summary>
    /// Assumption: child adn virtualVector as under dad in the Hierarchy.
    /// Rotates dad around child object to make virtual vector face the same direction as actual vector.
    /// The result of this operation is that the actual and virtual vector will face the same direction while keeping the global position of child the same
    /// </summary>
    /// <param name="child"></param>
    /// <param name="dad"></param>
    /// <param name="virtualVector"></param>
    /// <param name="actualVector"></param>
    public static void rotateDadtoAlignVirtualActualVectors(GameObject child, GameObject dad, Vector3 virtualVector, Vector3 actualVector)
    {
        RLogger.Log($"[AlignHelpers:rotateDadtoAlignVirtualVector] called.");
        Quaternion rotation = Quaternion.FromToRotation(virtualVector, actualVector);
        float angle; Vector3 axis;
        rotation.ToAngleAxis(out angle, out axis);
        dad.transform.RotateAround(child.transform.position, axis, angle);
        RLogger.Log($"rotated dad");
    }

    /// <summary>
    /// Rotates dad around the child object to make virtual vector face the same direction as actual vector on the xz plane.<br />
    /// Assumption: child and virtualVector are under dad in the Hierarchy.<br />
    /// The result of this operation is that the actual and virtual vector will face the same direction while keeping the global position of child the same
    /// </summary>
    /// <param name="child"></param>
    /// <param name="dad"></param>
    /// <param name="virtualVector"></param>
    /// <param name="actualVector"></param>
    public static void rotateDadtoAlignVirtualActualVectorsOnGroundPlane(GameObject child, GameObject dad, Vector3 virtualVector, Vector3 actualVector)
    {
        RLogger.Log($"[AlignHelpers:rotateDadtoAlignVirtualVector] called.");
        virtualVector.y = 0; actualVector.y = 0;
        Quaternion rotation = Quaternion.FromToRotation(virtualVector, actualVector);
        float angle; Vector3 axis;
        rotation.ToAngleAxis(out angle, out axis);
        dad.transform.RotateAround(child.transform.position, axis, angle);
        RLogger.Log($"rotated dad");
    }
}
