using System.IO;

namespace NIF.Parser.NiObjects
{
    /// <summary>
    /// The "T" suffix marks this body as active for translation and rotation.
    /// </summary>
    public class BhkRigidBodyT : BhkRigidBody
    {

        private BhkRigidBodyT(BhkRigidBody ancestor) : base(ancestor.ShapeReference, ancestor.Layer,
            ancestor.CollisionFilterFlags,
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
            ancestor.ConstraintRefs, ancestor.BodyFlags)
        {
        }

        public new static BhkRigidBodyT Parse(BinaryReader nifReader, string ownerObjectName, Header header)
        {
            var ancestor = BhkRigidBody.Parse(nifReader, ownerObjectName, header);
            return new BhkRigidBodyT(ancestor);
        }
    }
}