using System.IO;
using NIF.NiObjects.Enums;
using NIF.NiObjects.Structures;

namespace NIF.NiObjects
{
    /// <summary>
    /// The "T" suffix marks this body as active for translation and rotation.
    /// </summary>
    public class BhkRigidBodyT : BhkRigidBody
    {
        //There MUST be a better way of doing this...
        
        private BhkRigidBodyT(int shapeReference, SkyrimLayer layer, byte collisionFilterFlags, ushort group,
            HkResponseType responseType, ushort processContactCallbackDelay, Vector4 translation, HkQuaternion rotation,
            Vector4 linearVelocity, Vector4 angularVelocity, HkMatrix3 inertiaTensor, Vector4 center, float mass,
            float linearDamping, float angularDamping, float timeFactor, float gravityFactor, float friction,
            float rollingFrictionMultiplier, float restitution, float maxLinearVelocity, float maxAngularVelocity,
            float penetrationDepth, HkMotionType motionSystem, HkDeactivatorType deactivatorType,
            HkSolverDeactivation solverDeactivation, HkQualityType qualityType, byte autoRemoveLevel,
            byte responseModifierFlags, byte numShapeKeysInContactPoint, bool forceCollidedOntoPpu, uint numConstraints,
            int[] constraintRefs, uint bodyFlags) : base(shapeReference, layer, collisionFilterFlags, group,
            responseType, processContactCallbackDelay, translation, rotation, linearVelocity, angularVelocity,
            inertiaTensor, center, mass, linearDamping, angularDamping, timeFactor, gravityFactor, friction,
            rollingFrictionMultiplier, restitution, maxLinearVelocity, maxAngularVelocity, penetrationDepth,
            motionSystem, deactivatorType, solverDeactivation, qualityType, autoRemoveLevel, responseModifierFlags,
            numShapeKeysInContactPoint, forceCollidedOntoPpu, numConstraints, constraintRefs, bodyFlags)
        {
        }

        public new static BhkRigidBodyT Parse(BinaryReader nifReader, string ownerObjectName, Header header)
        {
            var ancestor = BhkRigidBody.Parse(nifReader, ownerObjectName, header);
            return new BhkRigidBodyT(ancestor.ShapeReference, ancestor.Layer, ancestor.CollisionFilterFlags,
                ancestor.Group,
                ancestor.ResponseType, ancestor.ProcessContactCallbackDelay, ancestor.Translation, ancestor.Rotation,
                ancestor.LinearVelocity, ancestor.AngularVelocity,
                ancestor.InertiaTensor, ancestor.Center, ancestor.Mass, ancestor.LinearDamping, ancestor.AngularDamping,
                ancestor.TimeFactor, ancestor.GravityFactor, ancestor.Friction,
                ancestor.RollingFrictionMultiplier, ancestor.Restitution, ancestor.MaxLinearVelocity,
                ancestor.MaxAngularVelocity, ancestor.PenetrationDepth,
                ancestor.MotionSystem, ancestor.DeactivatorType, ancestor.SolverDeactivation, ancestor.QualityType,
                ancestor.AutoRemoveLevel, ancestor.ResponseModifierFlags,
                ancestor.NumShapeKeysInContactPoint, ancestor.ForceCollidedOntoPPU, ancestor.NumConstraints,
                ancestor.ConstraintRefs, ancestor.BodyFlags);
        }
    }
}