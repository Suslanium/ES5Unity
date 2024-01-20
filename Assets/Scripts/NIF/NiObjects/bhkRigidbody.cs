using System;
using System.IO;
using NIF.NiObjects.Enums;
using NIF.NiObjects.Structures;

namespace NIF.NiObjects
{
    /// <summary>
    /// This is the default body type for all "normal" usable and static world objects. The "T" suffix marks this body as active for translation and rotation, a normal bhkRigidBody ignores those properties. Because the properties are equal, a bhkRigidBody may be renamed into a bhkRigidBodyT and vice-versa.
    /// </summary>
    public class BhkRigidBody : BhkEntity
    {
        //Duplicated info?
        // public new SkyrimLayer Layer { get; private set; }
        //
        // public new byte CollisionFilterFlags { get; private set; }
        //
        // public new ushort Group { get; private set; }
        //
        // public HkResponseType CollisionResponse { get; set; }
        //
        // public new ushort ProcessContactCallbackDelay { get; private set; }

        /// <summary>
        ///  A vector that moves the body by the specified amount. Only enabled in bhkRigidBodyT objects.
        /// </summary>
        public Vector4 Translation { get; private set; }

        /// <summary>
        /// The rotation Yaw/Pitch/Roll to apply to the body. Only enabled in bhkRigidBodyT objects.
        /// </summary>
        public HkQuaternion Rotation { get; private set; }

        public Vector4 LinearVelocity { get; private set; }

        public Vector4 AngularVelocity { get; private set; }

        /// <summary>
        /// Defines how the mass is distributed among the body, i.e. how difficult it is to rotate around any given axis.
        /// </summary>
        public HkMatrix3 InertiaTensor { get; private set; }

        /// <summary>
        /// The body's center of mass.
        /// </summary>
        public Vector4 Center { get; private set; }

        /// <summary>
        /// The body's mass in kg. A mass of zero represents an immovable object.
        /// </summary>
        public float Mass { get; private set; }

        /// <summary>
        /// Reduces the movement of the body over time. A value of 0.1 will remove 10% of the linear velocity every second.
        /// </summary>
        public float LinearDamping { get; private set; }

        /// <summary>
        /// Reduces the movement of the body over time. A value of 0.05 will remove 5% of the angular velocity every second.
        /// </summary>
        public float AngularDamping { get; private set; }

        public float TimeFactor { get; private set; }

        public float GravityFactor { get; private set; }

        /// <summary>
        /// How smooth its surfaces is and how easily it will slide along other bodies.
        /// </summary>
        public float Friction { get; private set; }

        public float RollingFrictionMultiplier { get; private set; }

        /// <summary>
        /// How "bouncy" the body is, i.e. how much energy it has after colliding. Less than 1.0 loses energy, greater than 1.0 gains energy.
        /// </summary>
        public float Restitution { get; private set; }

        public float MaxLinearVelocity { get; private set; }

        public float MaxAngularVelocity { get; private set; }

        /// <summary>
        /// The maximum allowed penetration for this object. This is a hint to the engine to see how much CPU the engine should invest to keep this object from penetrating.
        /// </summary>
        public float PenetrationDepth { get; private set; }

        public HkMotionType MotionSystem { get; private set; }

        public HkDeactivatorType DeactivatorType { get; private set; }

        public HkSolverDeactivation SolverDeactivation { get; private set; }

        /// <summary>
        /// The type of interaction with other objects.
        /// </summary>
        public HkQualityType QualityType { get; private set; }

        public byte AutoRemoveLevel { get; private set; }

        public byte ResponseModifierFlags { get; private set; }

        public byte NumShapeKeysInContactPoint { get; private set; }

        public bool ForceCollidedOntoPPU { get; private set; }

        public uint NumConstraints { get; private set; }

        public int[] ConstraintRefs { get; private set; }

        /// <summary>
        /// 1 = respond to wind
        /// </summary>
        public uint BodyFlags { get; private set; }

        private BhkRigidBody(int shapeReference, SkyrimLayer layer, byte collisionFilterFlags, ushort group,
            HkResponseType responseType, ushort processContactCallbackDelay) : base(shapeReference, layer,
            collisionFilterFlags, group, responseType, processContactCallbackDelay)
        {
        }

        protected BhkRigidBody(int shapeReference, SkyrimLayer layer, byte collisionFilterFlags, ushort group,
            HkResponseType responseType, ushort processContactCallbackDelay, Vector4 translation, HkQuaternion rotation,
            Vector4 linearVelocity, Vector4 angularVelocity, HkMatrix3 inertiaTensor, Vector4 center, float mass,
            float linearDamping, float angularDamping, float timeFactor, float gravityFactor, float friction,
            float rollingFrictionMultiplier, float restitution, float maxLinearVelocity, float maxAngularVelocity,
            float penetrationDepth, HkMotionType motionSystem, HkDeactivatorType deactivatorType,
            HkSolverDeactivation solverDeactivation, HkQualityType qualityType, byte autoRemoveLevel,
            byte responseModifierFlags, byte numShapeKeysInContactPoint, bool forceCollidedOntoPpu, uint numConstraints,
            int[] constraintRefs, uint bodyFlags) : base(shapeReference, layer, collisionFilterFlags, group,
            responseType, processContactCallbackDelay)
        {
            Translation = translation;
            Rotation = rotation;
            LinearVelocity = linearVelocity;
            AngularVelocity = angularVelocity;
            InertiaTensor = inertiaTensor;
            Center = center;
            Mass = mass;
            LinearDamping = linearDamping;
            AngularDamping = angularDamping;
            TimeFactor = timeFactor;
            GravityFactor = gravityFactor;
            Friction = friction;
            RollingFrictionMultiplier = rollingFrictionMultiplier;
            Restitution = restitution;
            MaxLinearVelocity = maxLinearVelocity;
            MaxAngularVelocity = maxAngularVelocity;
            PenetrationDepth = penetrationDepth;
            MotionSystem = motionSystem;
            DeactivatorType = deactivatorType;
            SolverDeactivation = solverDeactivation;
            QualityType = qualityType;
            AutoRemoveLevel = autoRemoveLevel;
            ResponseModifierFlags = responseModifierFlags;
            NumShapeKeysInContactPoint = numShapeKeysInContactPoint;
            ForceCollidedOntoPPU = forceCollidedOntoPpu;
            NumConstraints = numConstraints;
            ConstraintRefs = constraintRefs;
            BodyFlags = bodyFlags;
        }

        public new static BhkRigidBody Parse(BinaryReader nifReader, string ownerObjectName, Header header)
        {
            var ancestor = BhkEntity.Parse(nifReader, ownerObjectName, header);
            var bhkRigidBody = new BhkRigidBody(ancestor.ShapeReference, ancestor.Layer, ancestor.CollisionFilterFlags,
                ancestor.Group, ancestor.ResponseType, ancestor.ProcessContactCallbackDelay);
            if (!(Conditions.BsGteSky(header) && !Conditions.BsFo4(header)))
            {
                //Exception/skip
            }

            //Skipping unused/duplicated data
            nifReader.BaseStream.Seek(20, SeekOrigin.Current);
            bhkRigidBody.Translation = Vector4.Parse(nifReader);
            bhkRigidBody.Rotation = HkQuaternion.Parse(nifReader);
            bhkRigidBody.LinearVelocity = Vector4.Parse(nifReader);
            bhkRigidBody.AngularVelocity = Vector4.Parse(nifReader);
            bhkRigidBody.InertiaTensor = HkMatrix3.Parse(nifReader);
            bhkRigidBody.Center = Vector4.Parse(nifReader);
            bhkRigidBody.Mass = nifReader.ReadSingle();
            bhkRigidBody.LinearDamping = nifReader.ReadSingle();
            bhkRigidBody.AngularDamping = nifReader.ReadSingle();
            bhkRigidBody.TimeFactor = nifReader.ReadSingle();
            bhkRigidBody.GravityFactor = nifReader.ReadSingle();
            bhkRigidBody.Friction = nifReader.ReadSingle();
            bhkRigidBody.RollingFrictionMultiplier = nifReader.ReadSingle();
            bhkRigidBody.Restitution = nifReader.ReadSingle();
            bhkRigidBody.MaxLinearVelocity = nifReader.ReadSingle();
            bhkRigidBody.MaxAngularVelocity = nifReader.ReadSingle();
            bhkRigidBody.PenetrationDepth = nifReader.ReadSingle();
            var motionSystem = nifReader.ReadByte();
            bhkRigidBody.MotionSystem = (HkMotionType)motionSystem;
            var deactivatorType = nifReader.ReadByte();
            bhkRigidBody.DeactivatorType = (HkDeactivatorType)deactivatorType;
            var solverDeactivation = nifReader.ReadByte();
            bhkRigidBody.SolverDeactivation = (HkSolverDeactivation)solverDeactivation;
            var qualityType = nifReader.ReadByte();
            bhkRigidBody.QualityType = (HkQualityType)qualityType;
            bhkRigidBody.AutoRemoveLevel = nifReader.ReadByte();
            bhkRigidBody.ResponseModifierFlags = nifReader.ReadByte();
            bhkRigidBody.NumShapeKeysInContactPoint = nifReader.ReadByte();
            bhkRigidBody.ForceCollidedOntoPPU = nifReader.ReadBoolean();
            //Skipping unused byte array
            nifReader.BaseStream.Seek(12, SeekOrigin.Current);
            bhkRigidBody.NumConstraints = nifReader.ReadUInt32();
            bhkRigidBody.ConstraintRefs = NifReaderUtils.ReadRefArray(nifReader, bhkRigidBody.NumConstraints);
            bhkRigidBody.BodyFlags = header.BethesdaVersion >= 76 ? nifReader.ReadUInt16() : nifReader.ReadUInt32();

            return bhkRigidBody;
        }
    }
}