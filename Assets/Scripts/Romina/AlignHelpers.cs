using UnityEngine;

public static class AlignHelpers
{
    public static void moveDadToMakeChildMatchDestination(GameObject child, GameObject dad, Vector3 destination)
    {
        Vector3 diff = child.transform.position - dad.transform.position;
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
        direction.y = 0; direction.Normalize();
        Vector3 childFacing = child.transform.forward; childFacing.y = 0; childFacing.Normalize();
        float angle = Vector3.SignedAngle(childFacing, direction, Vector3.up);
        dad.transform.RotateAround(child.transform.position, Vector3.up, angle);
    }
}
