using System.Collections.Generic;
using System.Linq;

//We could do a linq-expr based typeof mapping with generic functions,
//  ... but the goal is to make this reasonably performant.
//  So we use a lot of regex bullshit and five miles of "generator" code instead.
//  Also note, most of our everything here should wind up inlined by the JITter.
//When adding things, be sure to make a Static version as well attached to the
//  static partial subclass named Static.
//-- Eirshy

namespace Eirshy.DSP.Rythmn {

    /// <summary>
    /// A wrapper struct for factory-paired EntityData that provides for easy
    /// fetching/checking of live references to Component data structs, and
    /// allows for effectively passing around all relevant components at once.
    /// <br />See remarks for a usage example.
    /// </summary>
    /// <remarks>
    /// Example:
    /// <c>
    /// <br />var fact = GameMain.data.factories.First();
    /// <br />var entr = new EntityRef(fact, 0);
    /// <br />ref var powerCons = ref entr.GetLive_PowerConsumerComponent();
    /// <br />powerCons.idleEnergyPerTick = 0;//remove idle power consumption
    /// </c>
    /// </remarks>
    public struct EntityRef {
        public PlanetFactory Factory { get; }
        public int EntityId { get; }

        #region Instance construction
        /// <summary>
        /// Creates a wrapper struct the given entityId in Factory that provides easy
        /// fetching/checking live references to Component data structs.
        /// </summary>
        /// <remarks> See this type itself's remarks for a usage example. </remarks>
        public EntityRef(PlanetFactory factory, int entityId) {
            Factory = factory;
            EntityId = entityId;
        }

        /// <summary>
        /// Creates a wrapper struct for the given entity in factory that provides easy
        /// fetching/checking live references to Component data structs.
        /// </summary>
        /// <remarks> See this type itself's remarks for a usage example. </remarks>
        public EntityRef(PlanetFactory factory, in EntityData entity) {
            Factory = factory;
            EntityId = entity.id;
        }

        #endregion
        #region EntityData GetLive_ and instance-only ReloadEntity();

        private ref EntityData Entity => ref Factory.entityPool[EntityId];

        /// <summary>
        /// Returns a ref to the live EntityData struct
        /// </summary>
        public ref EntityData GetLive_EntityData() => ref Factory.entityPool[EntityId];

        /// <summary>
        /// Returns a ref to the live EntityData struct
        /// </summary>
        public static ref EntityData GetLive_EntityData(PlanetFactory factory, int entityId) {
            return ref factory.entityPool[entityId];
        }

        #endregion
        #region ItemProto Helpers
        /// <summary>
        /// Whether our Entity is the given proto
        /// </summary>
        public bool IsItem(ItemProto proto) => Entity.protoId == proto.ID;
        /// <summary>
        /// Whether the entity is the given proto
        /// </summary>
        public static bool IsItem(in EntityData entity, ItemProto proto) => entity.protoId == proto.ID;

        /// <summary>
        /// Gets the ItemProto for our Entity.
        /// <br />
        /// Note that edits made to the ItemProto are not automatically propigated.
        /// You should only use this as a reference.
        /// </summary>
        public ItemProto GetItem() => LDB.items.Select(Entity.protoId);
        /// <summary>
        /// Gets the ItemProto for the entity.
        /// <br />
        /// Note that edits made to the ItemProto are not automatically propigated.
        /// You should only use this as a reference.
        /// </summary>
        public static ItemProto GetItem(in EntityData entity) => LDB.items.Select(entity.protoId);
        /// <summary>
        /// Gets the ItemProto for the entity.
        /// <br />
        /// Note that edits made to the ItemProto are not automatically propigated.
        /// You should only use this as a reference.
        /// </summary>
        /// <remarks>Provided as a LINQ helper</remarks>
        public static ItemProto GetItem(in EntityRef entr) => entr.GetItem();

        #endregion

        #region Get All -- ANY COMPONENT

        /// <summary>
        /// Creates an IEnum for all not-null entities in the current game
        /// </summary>
        public static IEnumerable<EntityRef> GetEntityRefs(GameData data) {
            return data.factories.Where(f=>f != null).SelectMany(GetEntityRefs);
        }

        /// <summary>
        /// Creates an IEnum for all not-null entities in the given factory
        /// </summary>
        public static IEnumerable<EntityRef> GetEntityRefs(PlanetFactory factory) {
            if(factory is null) return Enumerable.Empty<EntityRef>();
            return factory.entityPool
                .Where(ed=>ed.notNull)
                .Select(ed => new EntityRef(factory, ed.id))
            ;
        }

        #endregion

        //Basic-Instance -- Has_, HasActive_, GetLive_, CheckEntity, CheckComponent, CheckCircular
        #region Basic-Instance BeltComponent 
        /// <summary>
        /// True if our Entity's beltId does not point to the null BeltComponent
        /// </summary>
        public bool Has_BeltComponent => Entity.beltId != EntityData.Null.beltId;
        /// <summary>
        /// Whether our BeltComponent in Factory matches our Entity.
        /// </summary>
        /// <remarks>
        /// Equivalent to calling <c>CheckEntity</c> on the result of <c>GetLive_BeltComponent</c>
        /// <br />
        /// Provided first-class because this is the relationship the game itself checks (circa v0.9.24)
        /// </remarks>
        public bool HasActive_BeltComponent => Entity.id == Factory.cargoTraffic.beltPool[Entity.beltId].entityId;
        /// <summary>
        /// Gets a live reference to the BeltComponent this entity points to.
        /// <br />DO NOT edit it directly! Save it to a local ref-var first!
        /// </summary>
        public ref BeltComponent GetLive_BeltComponent() => ref Factory.cargoTraffic.beltPool[Entity.beltId];
        /// <summary>
        /// Checks whether the component matches our Entity
        /// </summary>
        public bool CheckEntity(ref BeltComponent cmp) => Entity.id == cmp.entityId;
        /// <summary>
        /// Checks whether our Entity matches the component
        /// </summary>
        public bool CheckComponent(ref BeltComponent cmp) => Entity.beltId == cmp.id;
        /// <summary>
        /// Checks whether both our Entity and the component match eachother.
        /// </summary>
        public bool CheckCircular(ref BeltComponent cmp) => Entity.id == cmp.entityId && Entity.beltId == cmp.id;

        #endregion
        #region Basic-Instance SplitterComponent 
        /// <summary>
        /// True if our Entity's splitterId does not point to the null SplitterComponent
        /// </summary>
        public bool Has_SplitterComponent => Entity.splitterId != EntityData.Null.splitterId;
        /// <summary>
        /// Whether our SplitterComponent in Factory matches our Entity.
        /// </summary>
        /// <remarks>
        /// Equivalent to calling <c>CheckEntity</c> on the result of <c>GetLive_SplitterComponent</c>
        /// <br />
        /// Provided first-class because this is the relationship the game itself checks (circa v0.9.24)
        /// </remarks>
        public bool HasActive_SplitterComponent => Entity.id == Factory.cargoTraffic.splitterPool[Entity.splitterId].entityId;
        /// <summary>
        /// Gets a live reference to the SplitterComponent this entity points to.
        /// <br />DO NOT edit it directly! Save it to a local ref-var first!
        /// </summary>
        public ref SplitterComponent GetLive_SplitterComponent() => ref Factory.cargoTraffic.splitterPool[Entity.splitterId];
        /// <summary>
        /// Checks whether the component matches our Entity
        /// </summary>
        public bool CheckEntity(ref SplitterComponent cmp) => Entity.id == cmp.entityId;
        /// <summary>
        /// Checks whether our Entity matches the component
        /// </summary>
        public bool CheckComponent(ref SplitterComponent cmp) => Entity.splitterId == cmp.id;
        /// <summary>
        /// Checks whether both our Entity and the component match eachother.
        /// </summary>
        public bool CheckCircular(ref SplitterComponent cmp) => Entity.id == cmp.entityId && Entity.splitterId == cmp.id;

        #endregion
        #region Basic-Instance MonitorComponent 
        /// <summary>
        /// True if our Entity's monitorId does not point to the null MonitorComponent
        /// </summary>
        public bool Has_MonitorComponent => Entity.monitorId != EntityData.Null.monitorId;
        /// <summary>
        /// Whether our MonitorComponent in Factory matches our Entity.
        /// </summary>
        /// <remarks>
        /// Equivalent to calling <c>CheckEntity</c> on the result of <c>GetLive_MonitorComponent</c>
        /// <br />
        /// Provided first-class because this is the relationship the game itself checks (circa v0.9.24)
        /// </remarks>
        public bool HasActive_MonitorComponent => Entity.id == Factory.cargoTraffic.monitorPool[Entity.monitorId].entityId;
        /// <summary>
        /// Gets a live reference to the MonitorComponent this entity points to.
        /// <br />DO NOT edit it directly! Save it to a local ref-var first!
        /// </summary>
        public ref MonitorComponent GetLive_MonitorComponent() => ref Factory.cargoTraffic.monitorPool[Entity.monitorId];
        /// <summary>
        /// Checks whether the component matches our Entity
        /// </summary>
        public bool CheckEntity(ref MonitorComponent cmp) => Entity.id == cmp.entityId;
        /// <summary>
        /// Checks whether our Entity matches the component
        /// </summary>
        public bool CheckComponent(ref MonitorComponent cmp) => Entity.monitorId == cmp.id;
        /// <summary>
        /// Checks whether both our Entity and the component match eachother.
        /// </summary>
        public bool CheckCircular(ref MonitorComponent cmp) => Entity.id == cmp.entityId && Entity.monitorId == cmp.id;

        #endregion
        #region Basic-Instance SpraycoaterComponent 
        /// <summary>
        /// True if our Entity's spraycoaterId does not point to the null SpraycoaterComponent
        /// </summary>
        public bool Has_SpraycoaterComponent => Entity.spraycoaterId != EntityData.Null.spraycoaterId;
        /// <summary>
        /// Whether our SpraycoaterComponent in Factory matches our Entity.
        /// </summary>
        /// <remarks>
        /// Equivalent to calling <c>CheckEntity</c> on the result of <c>GetLive_SpraycoaterComponent</c>
        /// <br />
        /// Provided first-class because this is the relationship the game itself checks (circa v0.9.24)
        /// </remarks>
        public bool HasActive_SpraycoaterComponent => Entity.id == Factory.cargoTraffic.spraycoaterPool[Entity.spraycoaterId].entityId;
        /// <summary>
        /// Gets a live reference to the SpraycoaterComponent this entity points to.
        /// <br />DO NOT edit it directly! Save it to a local ref-var first!
        /// </summary>
        public ref SpraycoaterComponent GetLive_SpraycoaterComponent() => ref Factory.cargoTraffic.spraycoaterPool[Entity.spraycoaterId];
        /// <summary>
        /// Checks whether the component matches our Entity
        /// </summary>
        public bool CheckEntity(ref SpraycoaterComponent cmp) => Entity.id == cmp.entityId;
        /// <summary>
        /// Checks whether our Entity matches the component
        /// </summary>
        public bool CheckComponent(ref SpraycoaterComponent cmp) => Entity.spraycoaterId == cmp.id;
        /// <summary>
        /// Checks whether both our Entity and the component match eachother.
        /// </summary>
        public bool CheckCircular(ref SpraycoaterComponent cmp) => Entity.id == cmp.entityId && Entity.spraycoaterId == cmp.id;

        #endregion
        #region Basic-Instance PilerComponent 
        /// <summary>
        /// True if our Entity's pilerId does not point to the null PilerComponent
        /// </summary>
        public bool Has_PilerComponent => Entity.pilerId != EntityData.Null.pilerId;
        /// <summary>
        /// Whether our PilerComponent in Factory matches our Entity.
        /// </summary>
        /// <remarks>
        /// Equivalent to calling <c>CheckEntity</c> on the result of <c>GetLive_PilerComponent</c>
        /// <br />
        /// Provided first-class because this is the relationship the game itself checks (circa v0.9.24)
        /// </remarks>
        public bool HasActive_PilerComponent => Entity.id == Factory.cargoTraffic.pilerPool[Entity.pilerId].entityId;
        /// <summary>
        /// Gets a live reference to the PilerComponent this entity points to.
        /// <br />DO NOT edit it directly! Save it to a local ref-var first!
        /// </summary>
        public ref PilerComponent GetLive_PilerComponent() => ref Factory.cargoTraffic.pilerPool[Entity.pilerId];
        /// <summary>
        /// Checks whether the component matches our Entity
        /// </summary>
        public bool CheckEntity(ref PilerComponent cmp) => Entity.id == cmp.entityId;
        /// <summary>
        /// Checks whether our Entity matches the component
        /// </summary>
        public bool CheckComponent(ref PilerComponent cmp) => Entity.pilerId == cmp.id;
        /// <summary>
        /// Checks whether both our Entity and the component match eachother.
        /// </summary>
        public bool CheckCircular(ref PilerComponent cmp) => Entity.id == cmp.entityId && Entity.pilerId == cmp.id;

        #endregion
        #region Basic-Instance SpeakerComponent 
        /// <summary>
        /// True if our Entity's speakerId does not point to the null SpeakerComponent
        /// </summary>
        public bool Has_SpeakerComponent => Entity.speakerId != EntityData.Null.speakerId;
        /// <summary>
        /// Whether our SpeakerComponent in Factory matches our Entity.
        /// </summary>
        /// <remarks>
        /// Equivalent to calling <c>CheckEntity</c> on the result of <c>GetLive_SpeakerComponent</c>
        /// <br />
        /// Provided first-class because this is the relationship the game itself checks (circa v0.9.24)
        /// </remarks>
        public bool HasActive_SpeakerComponent => Entity.id == Factory.digitalSystem.speakerPool[Entity.speakerId].entityId;
        /// <summary>
        /// Gets a live reference to the SpeakerComponent this entity points to.
        /// <br />DO NOT edit it directly! Save it to a local ref-var first!
        /// </summary>
        public ref SpeakerComponent GetLive_SpeakerComponent() => ref Factory.digitalSystem.speakerPool[Entity.speakerId];
        /// <summary>
        /// Checks whether the component matches our Entity
        /// </summary>
        public bool CheckEntity(ref SpeakerComponent cmp) => Entity.id == cmp.entityId;
        /// <summary>
        /// Checks whether our Entity matches the component
        /// </summary>
        public bool CheckComponent(ref SpeakerComponent cmp) => Entity.speakerId == cmp.id;
        /// <summary>
        /// Checks whether both our Entity and the component match eachother.
        /// </summary>
        public bool CheckCircular(ref SpeakerComponent cmp) => Entity.id == cmp.entityId && Entity.speakerId == cmp.id;

        #endregion
        #region Basic-Instance StorageComponent 
        /// <summary>
        /// True if our Entity's storageId does not point to the null StorageComponent
        /// </summary>
        public bool Has_StorageComponent => Entity.storageId != EntityData.Null.storageId;
        /// <summary>
        /// Whether our StorageComponent in Factory matches our Entity.
        /// </summary>
        /// <remarks>
        /// Equivalent to calling <c>CheckEntity</c> on the result of <c>GetLive_StorageComponent</c>
        /// <br />
        /// Provided first-class because this is the relationship the game itself checks (circa v0.9.24)
        /// </remarks>
        public bool HasActive_StorageComponent => Entity.id == Factory.factoryStorage.storagePool[Entity.storageId].entityId;
        /// <summary>
        /// Gets a live reference to the StorageComponent this entity points to.
        /// <br />DO NOT edit it directly! Save it to a local ref-var first!
        /// </summary>
        public ref StorageComponent GetLive_StorageComponent() => ref Factory.factoryStorage.storagePool[Entity.storageId];
        /// <summary>
        /// Checks whether the component matches our Entity
        /// </summary>
        public bool CheckEntity(ref StorageComponent cmp) => Entity.id == cmp.entityId;
        /// <summary>
        /// Checks whether our Entity matches the component
        /// </summary>
        public bool CheckComponent(ref StorageComponent cmp) => Entity.storageId == cmp.id;
        /// <summary>
        /// Checks whether both our Entity and the component match eachother.
        /// </summary>
        public bool CheckCircular(ref StorageComponent cmp) => Entity.id == cmp.entityId && Entity.storageId == cmp.id;

        #endregion
        #region Basic-Instance TankComponent 
        /// <summary>
        /// True if our Entity's tankId does not point to the null TankComponent
        /// </summary>
        public bool Has_TankComponent => Entity.tankId != EntityData.Null.tankId;
        /// <summary>
        /// Whether our TankComponent in Factory matches our Entity.
        /// </summary>
        /// <remarks>
        /// Equivalent to calling <c>CheckEntity</c> on the result of <c>GetLive_TankComponent</c>
        /// <br />
        /// Provided first-class because this is the relationship the game itself checks (circa v0.9.24)
        /// </remarks>
        public bool HasActive_TankComponent => Entity.id == Factory.factoryStorage.tankPool[Entity.tankId].entityId;
        /// <summary>
        /// Gets a live reference to the TankComponent this entity points to.
        /// <br />DO NOT edit it directly! Save it to a local ref-var first!
        /// </summary>
        public ref TankComponent GetLive_TankComponent() => ref Factory.factoryStorage.tankPool[Entity.tankId];
        /// <summary>
        /// Checks whether the component matches our Entity
        /// </summary>
        public bool CheckEntity(ref TankComponent cmp) => Entity.id == cmp.entityId;
        /// <summary>
        /// Checks whether our Entity matches the component
        /// </summary>
        public bool CheckComponent(ref TankComponent cmp) => Entity.tankId == cmp.id;
        /// <summary>
        /// Checks whether both our Entity and the component match eachother.
        /// </summary>
        public bool CheckCircular(ref TankComponent cmp) => Entity.id == cmp.entityId && Entity.tankId == cmp.id;

        #endregion
        #region Basic-Instance MinerComponent 
        /// <summary>
        /// True if our Entity's minerId does not point to the null MinerComponent
        /// </summary>
        public bool Has_MinerComponent => Entity.minerId != EntityData.Null.minerId;
        /// <summary>
        /// Whether our MinerComponent in Factory matches our Entity.
        /// </summary>
        /// <remarks>
        /// Equivalent to calling <c>CheckEntity</c> on the result of <c>GetLive_MinerComponent</c>
        /// <br />
        /// Provided first-class because this is the relationship the game itself checks (circa v0.9.24)
        /// </remarks>
        public bool HasActive_MinerComponent => Entity.id == Factory.factorySystem.minerPool[Entity.minerId].entityId;
        /// <summary>
        /// Gets a live reference to the MinerComponent this entity points to.
        /// <br />DO NOT edit it directly! Save it to a local ref-var first!
        /// </summary>
        public ref MinerComponent GetLive_MinerComponent() => ref Factory.factorySystem.minerPool[Entity.minerId];
        /// <summary>
        /// Checks whether the component matches our Entity
        /// </summary>
        public bool CheckEntity(ref MinerComponent cmp) => Entity.id == cmp.entityId;
        /// <summary>
        /// Checks whether our Entity matches the component
        /// </summary>
        public bool CheckComponent(ref MinerComponent cmp) => Entity.minerId == cmp.id;
        /// <summary>
        /// Checks whether both our Entity and the component match eachother.
        /// </summary>
        public bool CheckCircular(ref MinerComponent cmp) => Entity.id == cmp.entityId && Entity.minerId == cmp.id;

        #endregion
        #region Basic-Instance InserterComponent 
        /// <summary>
        /// True if our Entity's inserterId does not point to the null InserterComponent
        /// </summary>
        public bool Has_InserterComponent => Entity.inserterId != EntityData.Null.inserterId;
        /// <summary>
        /// Whether our InserterComponent in Factory matches our Entity.
        /// </summary>
        /// <remarks>
        /// Equivalent to calling <c>CheckEntity</c> on the result of <c>GetLive_InserterComponent</c>
        /// <br />
        /// Provided first-class because this is the relationship the game itself checks (circa v0.9.24)
        /// </remarks>
        public bool HasActive_InserterComponent => Entity.id == Factory.factorySystem.inserterPool[Entity.inserterId].entityId;
        /// <summary>
        /// Gets a live reference to the InserterComponent this entity points to.
        /// <br />DO NOT edit it directly! Save it to a local ref-var first!
        /// </summary>
        public ref InserterComponent GetLive_InserterComponent() => ref Factory.factorySystem.inserterPool[Entity.inserterId];
        /// <summary>
        /// Checks whether the component matches our Entity
        /// </summary>
        public bool CheckEntity(ref InserterComponent cmp) => Entity.id == cmp.entityId;
        /// <summary>
        /// Checks whether our Entity matches the component
        /// </summary>
        public bool CheckComponent(ref InserterComponent cmp) => Entity.inserterId == cmp.id;
        /// <summary>
        /// Checks whether both our Entity and the component match eachother.
        /// </summary>
        public bool CheckCircular(ref InserterComponent cmp) => Entity.id == cmp.entityId && Entity.inserterId == cmp.id;

        #endregion
        #region Basic-Instance AssemblerComponent 
        /// <summary>
        /// True if our Entity's assemblerId does not point to the null AssemblerComponent
        /// </summary>
        public bool Has_AssemblerComponent => Entity.assemblerId != EntityData.Null.assemblerId;
        /// <summary>
        /// Whether our AssemblerComponent in Factory matches our Entity.
        /// </summary>
        /// <remarks>
        /// Equivalent to calling <c>CheckEntity</c> on the result of <c>GetLive_AssemblerComponent</c>
        /// <br />
        /// Provided first-class because this is the relationship the game itself checks (circa v0.9.24)
        /// </remarks>
        public bool HasActive_AssemblerComponent => Entity.id == Factory.factorySystem.assemblerPool[Entity.assemblerId].entityId;
        /// <summary>
        /// Gets a live reference to the AssemblerComponent this entity points to.
        /// <br />DO NOT edit it directly! Save it to a local ref-var first!
        /// </summary>
        public ref AssemblerComponent GetLive_AssemblerComponent() => ref Factory.factorySystem.assemblerPool[Entity.assemblerId];
        /// <summary>
        /// Checks whether the component matches our Entity
        /// </summary>
        public bool CheckEntity(ref AssemblerComponent cmp) => Entity.id == cmp.entityId;
        /// <summary>
        /// Checks whether our Entity matches the component
        /// </summary>
        public bool CheckComponent(ref AssemblerComponent cmp) => Entity.assemblerId == cmp.id;
        /// <summary>
        /// Checks whether both our Entity and the component match eachother.
        /// </summary>
        public bool CheckCircular(ref AssemblerComponent cmp) => Entity.id == cmp.entityId && Entity.assemblerId == cmp.id;

        #endregion
        #region Basic-Instance FractionatorComponent 
        /// <summary>
        /// True if our Entity's fractionatorId does not point to the null FractionatorComponent
        /// </summary>
        public bool Has_FractionatorComponent => Entity.fractionatorId != EntityData.Null.fractionatorId;
        /// <summary>
        /// Whether our FractionatorComponent in Factory matches our Entity.
        /// </summary>
        /// <remarks>
        /// Equivalent to calling <c>CheckEntity</c> on the result of <c>GetLive_FractionatorComponent</c>
        /// <br />
        /// Provided first-class because this is the relationship the game itself checks (circa v0.9.24)
        /// </remarks>
        public bool HasActive_FractionatorComponent => Entity.id == Factory.factorySystem.fractionatorPool[Entity.fractionatorId].entityId;
        /// <summary>
        /// Gets a live reference to the FractionatorComponent this entity points to.
        /// <br />DO NOT edit it directly! Save it to a local ref-var first!
        /// </summary>
        public ref FractionatorComponent GetLive_FractionatorComponent() => ref Factory.factorySystem.fractionatorPool[Entity.fractionatorId];
        /// <summary>
        /// Checks whether the component matches our Entity
        /// </summary>
        public bool CheckEntity(ref FractionatorComponent cmp) => Entity.id == cmp.entityId;
        /// <summary>
        /// Checks whether our Entity matches the component
        /// </summary>
        public bool CheckComponent(ref FractionatorComponent cmp) => Entity.fractionatorId == cmp.id;
        /// <summary>
        /// Checks whether both our Entity and the component match eachother.
        /// </summary>
        public bool CheckCircular(ref FractionatorComponent cmp) => Entity.id == cmp.entityId && Entity.fractionatorId == cmp.id;

        #endregion
        #region Basic-Instance EjectorComponent 
        /// <summary>
        /// True if our Entity's ejectorId does not point to the null EjectorComponent
        /// </summary>
        public bool Has_EjectorComponent => Entity.ejectorId != EntityData.Null.ejectorId;
        /// <summary>
        /// Whether our EjectorComponent in Factory matches our Entity.
        /// </summary>
        /// <remarks>
        /// Equivalent to calling <c>CheckEntity</c> on the result of <c>GetLive_EjectorComponent</c>
        /// <br />
        /// Provided first-class because this is the relationship the game itself checks (circa v0.9.24)
        /// </remarks>
        public bool HasActive_EjectorComponent => Entity.id == Factory.factorySystem.ejectorPool[Entity.ejectorId].entityId;
        /// <summary>
        /// Gets a live reference to the EjectorComponent this entity points to.
        /// <br />DO NOT edit it directly! Save it to a local ref-var first!
        /// </summary>
        public ref EjectorComponent GetLive_EjectorComponent() => ref Factory.factorySystem.ejectorPool[Entity.ejectorId];
        /// <summary>
        /// Checks whether the component matches our Entity
        /// </summary>
        public bool CheckEntity(ref EjectorComponent cmp) => Entity.id == cmp.entityId;
        /// <summary>
        /// Checks whether our Entity matches the component
        /// </summary>
        public bool CheckComponent(ref EjectorComponent cmp) => Entity.ejectorId == cmp.id;
        /// <summary>
        /// Checks whether both our Entity and the component match eachother.
        /// </summary>
        public bool CheckCircular(ref EjectorComponent cmp) => Entity.id == cmp.entityId && Entity.ejectorId == cmp.id;

        #endregion
        #region Basic-Instance SiloComponent 
        /// <summary>
        /// True if our Entity's siloId does not point to the null SiloComponent
        /// </summary>
        public bool Has_SiloComponent => Entity.siloId != EntityData.Null.siloId;
        /// <summary>
        /// Whether our SiloComponent in Factory matches our Entity.
        /// </summary>
        /// <remarks>
        /// Equivalent to calling <c>CheckEntity</c> on the result of <c>GetLive_SiloComponent</c>
        /// <br />
        /// Provided first-class because this is the relationship the game itself checks (circa v0.9.24)
        /// </remarks>
        public bool HasActive_SiloComponent => Entity.id == Factory.factorySystem.siloPool[Entity.siloId].entityId;
        /// <summary>
        /// Gets a live reference to the SiloComponent this entity points to.
        /// <br />DO NOT edit it directly! Save it to a local ref-var first!
        /// </summary>
        public ref SiloComponent GetLive_SiloComponent() => ref Factory.factorySystem.siloPool[Entity.siloId];
        /// <summary>
        /// Checks whether the component matches our Entity
        /// </summary>
        public bool CheckEntity(ref SiloComponent cmp) => Entity.id == cmp.entityId;
        /// <summary>
        /// Checks whether our Entity matches the component
        /// </summary>
        public bool CheckComponent(ref SiloComponent cmp) => Entity.siloId == cmp.id;
        /// <summary>
        /// Checks whether both our Entity and the component match eachother.
        /// </summary>
        public bool CheckCircular(ref SiloComponent cmp) => Entity.id == cmp.entityId && Entity.siloId == cmp.id;

        #endregion
        #region Basic-Instance LabComponent 
        /// <summary>
        /// True if our Entity's labId does not point to the null LabComponent
        /// </summary>
        public bool Has_LabComponent => Entity.labId != EntityData.Null.labId;
        /// <summary>
        /// Whether our LabComponent in Factory matches our Entity.
        /// </summary>
        /// <remarks>
        /// Equivalent to calling <c>CheckEntity</c> on the result of <c>GetLive_LabComponent</c>
        /// <br />
        /// Provided first-class because this is the relationship the game itself checks (circa v0.9.24)
        /// </remarks>
        public bool HasActive_LabComponent => Entity.id == Factory.factorySystem.labPool[Entity.labId].entityId;
        /// <summary>
        /// Gets a live reference to the LabComponent this entity points to.
        /// <br />DO NOT edit it directly! Save it to a local ref-var first!
        /// </summary>
        public ref LabComponent GetLive_LabComponent() => ref Factory.factorySystem.labPool[Entity.labId];
        /// <summary>
        /// Checks whether the component matches our Entity
        /// </summary>
        public bool CheckEntity(ref LabComponent cmp) => Entity.id == cmp.entityId;
        /// <summary>
        /// Checks whether our Entity matches the component
        /// </summary>
        public bool CheckComponent(ref LabComponent cmp) => Entity.labId == cmp.id;
        /// <summary>
        /// Checks whether both our Entity and the component match eachother.
        /// </summary>
        public bool CheckCircular(ref LabComponent cmp) => Entity.id == cmp.entityId && Entity.labId == cmp.id;

        #endregion
        #region Basic-Instance StationComponent 
        /// <summary>
        /// True if our Entity's stationId does not point to the null StationComponent
        /// </summary>
        public bool Has_StationComponent => Entity.stationId != EntityData.Null.stationId;
        /// <summary>
        /// Whether our StationComponent in Factory matches our Entity.
        /// </summary>
        /// <remarks>
        /// Equivalent to calling <c>CheckEntity</c> on the result of <c>GetLive_StationComponent</c>
        /// <br />
        /// Provided first-class because this is the relationship the game itself checks (circa v0.9.24)
        /// </remarks>
        public bool HasActive_StationComponent => Entity.id == Factory.transport.stationPool[Entity.stationId].entityId;
        /// <summary>
        /// Gets a live reference to the StationComponent this entity points to.
        /// <br />DO NOT edit it directly! Save it to a local ref-var first!
        /// </summary>
        public ref StationComponent GetLive_StationComponent() => ref Factory.transport.stationPool[Entity.stationId];
        /// <summary>
        /// Checks whether the component matches our Entity
        /// </summary>
        public bool CheckEntity(ref StationComponent cmp) => Entity.id == cmp.entityId;
        /// <summary>
        /// Checks whether our Entity matches the component
        /// </summary>
        public bool CheckComponent(ref StationComponent cmp) => Entity.stationId == cmp.id;
        /// <summary>
        /// Checks whether both our Entity and the component match eachother.
        /// </summary>
        public bool CheckCircular(ref StationComponent cmp) => Entity.id == cmp.entityId && Entity.stationId == cmp.id;

        #endregion
        #region Basic-Instance PowerGeneratorComponent 
        /// <summary>
        /// True if our Entity's powerGenId does not point to the null PowerGeneratorComponent
        /// </summary>
        public bool Has_PowerGeneratorComponent => Entity.powerGenId != EntityData.Null.powerGenId;
        /// <summary>
        /// Whether our PowerGeneratorComponent in Factory matches our Entity.
        /// </summary>
        /// <remarks>
        /// Equivalent to calling <c>CheckEntity</c> on the result of <c>GetLive_PowerGeneratorComponent</c>
        /// <br />
        /// Provided first-class because this is the relationship the game itself checks (circa v0.9.24)
        /// </remarks>
        public bool HasActive_PowerGeneratorComponent => Entity.id == Factory.powerSystem.genPool[Entity.powerGenId].entityId;
        /// <summary>
        /// Gets a live reference to the PowerGeneratorComponent this entity points to.
        /// <br />DO NOT edit it directly! Save it to a local ref-var first!
        /// </summary>
        public ref PowerGeneratorComponent GetLive_PowerGeneratorComponent() => ref Factory.powerSystem.genPool[Entity.powerGenId];
        /// <summary>
        /// Checks whether the component matches our Entity
        /// </summary>
        public bool CheckEntity(ref PowerGeneratorComponent cmp) => Entity.id == cmp.entityId;
        /// <summary>
        /// Checks whether our Entity matches the component
        /// </summary>
        public bool CheckComponent(ref PowerGeneratorComponent cmp) => Entity.powerGenId == cmp.id;
        /// <summary>
        /// Checks whether both our Entity and the component match eachother.
        /// </summary>
        public bool CheckCircular(ref PowerGeneratorComponent cmp) => Entity.id == cmp.entityId && Entity.powerGenId == cmp.id;

        #endregion
        #region Basic-Instance PowerNodeComponent 
        /// <summary>
        /// True if our Entity's powerNodeId does not point to the null PowerNodeComponent
        /// </summary>
        public bool Has_PowerNodeComponent => Entity.powerNodeId != EntityData.Null.powerNodeId;
        /// <summary>
        /// Whether our PowerNodeComponent in Factory matches our Entity.
        /// </summary>
        /// <remarks>
        /// Equivalent to calling <c>CheckEntity</c> on the result of <c>GetLive_PowerNodeComponent</c>
        /// <br />
        /// Provided first-class because this is the relationship the game itself checks (circa v0.9.24)
        /// </remarks>
        public bool HasActive_PowerNodeComponent => Entity.id == Factory.powerSystem.nodePool[Entity.powerNodeId].entityId;
        /// <summary>
        /// Gets a live reference to the PowerNodeComponent this entity points to.
        /// <br />DO NOT edit it directly! Save it to a local ref-var first!
        /// </summary>
        public ref PowerNodeComponent GetLive_PowerNodeComponent() => ref Factory.powerSystem.nodePool[Entity.powerNodeId];
        /// <summary>
        /// Checks whether the component matches our Entity
        /// </summary>
        public bool CheckEntity(ref PowerNodeComponent cmp) => Entity.id == cmp.entityId;
        /// <summary>
        /// Checks whether our Entity matches the component
        /// </summary>
        public bool CheckComponent(ref PowerNodeComponent cmp) => Entity.powerNodeId == cmp.id;
        /// <summary>
        /// Checks whether both our Entity and the component match eachother.
        /// </summary>
        public bool CheckCircular(ref PowerNodeComponent cmp) => Entity.id == cmp.entityId && Entity.powerNodeId == cmp.id;

        #endregion
        #region Basic-Instance PowerConsumerComponent 
        /// <summary>
        /// True if our Entity's powerConId does not point to the null PowerConsumerComponent
        /// </summary>
        public bool Has_PowerConsumerComponent => Entity.powerConId != EntityData.Null.powerConId;
        /// <summary>
        /// Whether our PowerConsumerComponent in Factory matches our Entity.
        /// </summary>
        /// <remarks>
        /// Equivalent to calling <c>CheckEntity</c> on the result of <c>GetLive_PowerConsumerComponent</c>
        /// <br />
        /// Provided first-class because this is the relationship the game itself checks (circa v0.9.24)
        /// </remarks>
        public bool HasActive_PowerConsumerComponent => Entity.id == Factory.powerSystem.consumerPool[Entity.powerConId].entityId;
        /// <summary>
        /// Gets a live reference to the PowerConsumerComponent this entity points to.
        /// <br />DO NOT edit it directly! Save it to a local ref-var first!
        /// </summary>
        public ref PowerConsumerComponent GetLive_PowerConsumerComponent() => ref Factory.powerSystem.consumerPool[Entity.powerConId];
        /// <summary>
        /// Checks whether the component matches our Entity
        /// </summary>
        public bool CheckEntity(ref PowerConsumerComponent cmp) => Entity.id == cmp.entityId;
        /// <summary>
        /// Checks whether our Entity matches the component
        /// </summary>
        public bool CheckComponent(ref PowerConsumerComponent cmp) => Entity.powerConId == cmp.id;
        /// <summary>
        /// Checks whether both our Entity and the component match eachother.
        /// </summary>
        public bool CheckCircular(ref PowerConsumerComponent cmp) => Entity.id == cmp.entityId && Entity.powerConId == cmp.id;

        #endregion
        #region Basic-Instance PowerAccumulatorComponent 
        /// <summary>
        /// True if our Entity's powerAccId does not point to the null PowerAccumulatorComponent
        /// </summary>
        public bool Has_PowerAccumulatorComponent => Entity.powerAccId != EntityData.Null.powerAccId;
        /// <summary>
        /// Whether our PowerAccumulatorComponent in Factory matches our Entity.
        /// </summary>
        /// <remarks>
        /// Equivalent to calling <c>CheckEntity</c> on the result of <c>GetLive_PowerAccumulatorComponent</c>
        /// <br />
        /// Provided first-class because this is the relationship the game itself checks (circa v0.9.24)
        /// </remarks>
        public bool HasActive_PowerAccumulatorComponent => Entity.id == Factory.powerSystem.accPool[Entity.powerAccId].entityId;
        /// <summary>
        /// Gets a live reference to the PowerAccumulatorComponent this entity points to.
        /// <br />DO NOT edit it directly! Save it to a local ref-var first!
        /// </summary>
        public ref PowerAccumulatorComponent GetLive_PowerAccumulatorComponent() => ref Factory.powerSystem.accPool[Entity.powerAccId];
        /// <summary>
        /// Checks whether the component matches our Entity
        /// </summary>
        public bool CheckEntity(ref PowerAccumulatorComponent cmp) => Entity.id == cmp.entityId;
        /// <summary>
        /// Checks whether our Entity matches the component
        /// </summary>
        public bool CheckComponent(ref PowerAccumulatorComponent cmp) => Entity.powerAccId == cmp.id;
        /// <summary>
        /// Checks whether both our Entity and the component match eachother.
        /// </summary>
        public bool CheckCircular(ref PowerAccumulatorComponent cmp) => Entity.id == cmp.entityId && Entity.powerAccId == cmp.id;

        #endregion
        #region Basic-Instance PowerExchangerComponent 
        /// <summary>
        /// True if our Entity's powerExcId does not point to the null PowerExchangerComponent
        /// </summary>
        public bool Has_PowerExchangerComponent => Entity.powerExcId != EntityData.Null.powerExcId;
        /// <summary>
        /// Whether our PowerExchangerComponent in Factory matches our Entity.
        /// </summary>
        /// <remarks>
        /// Equivalent to calling <c>CheckEntity</c> on the result of <c>GetLive_PowerExchangerComponent</c>
        /// <br />
        /// Provided first-class because this is the relationship the game itself checks (circa v0.9.24)
        /// </remarks>
        public bool HasActive_PowerExchangerComponent => Entity.id == Factory.powerSystem.excPool[Entity.powerExcId].entityId;
        /// <summary>
        /// Gets a live reference to the PowerExchangerComponent this entity points to.
        /// <br />DO NOT edit it directly! Save it to a local ref-var first!
        /// </summary>
        public ref PowerExchangerComponent GetLive_PowerExchangerComponent() => ref Factory.powerSystem.excPool[Entity.powerExcId];
        /// <summary>
        /// Checks whether the component matches our Entity
        /// </summary>
        public bool CheckEntity(ref PowerExchangerComponent cmp) => Entity.id == cmp.entityId;
        /// <summary>
        /// Checks whether our Entity matches the component
        /// </summary>
        public bool CheckComponent(ref PowerExchangerComponent cmp) => Entity.powerExcId == cmp.id;
        /// <summary>
        /// Checks whether both our Entity and the component match eachother.
        /// </summary>
        public bool CheckCircular(ref PowerExchangerComponent cmp) => Entity.id == cmp.entityId && Entity.powerExcId == cmp.id;

        #endregion

        //Basic-Static -- Uses_, UsesActive_, GetLive_, CheckEntity, CheckComponent, CheckCircular
        #region Basic-Static BeltComponent 
        /// <summary>
        /// True if the entity's beltId does not point to the null BeltComponent
        /// </summary>
        /// <remarks></remarks>
        public static bool Uses_BeltComponent(in EntityData entity) => entity.beltId != EntityData.Null.beltId;
        /// <summary>
        /// True if the entity's beltId does not point to the null BeltComponent
        /// </summary>
        /// <remarks>Provided strictly for LINQ convenience; prefer the pass-by-ref version.</remarks>
        public static bool Uses_BeltComponent(EntityData entity) => entity.beltId != EntityData.Null.beltId;
        /// <summary>
        /// True if the entity's BeltComponent in the factory matches the entity.
        /// </summary>
        /// <remarks>
        /// Equivalent to calling <c>CheckEntity</c> on the result of <c>GetLive_BeltComponent</c>
        /// <br />
        /// Provided first-class because this is the relationship the game itself checks (circa v0.9.24)
        /// </remarks>
        public static bool UsesActive_BeltComponent(PlanetFactory factory, in EntityData entity) {
            return entity.id == factory.cargoTraffic.beltPool[entity.beltId].entityId;
        }
        /// <summary>
        /// Gets a live reference to entity's BeltComponent in factory.
        /// </summary>
        public static ref BeltComponent GetLive_BeltComponent(PlanetFactory factory, in EntityData entity) {
            return ref factory.cargoTraffic.beltPool[entity.beltId];
        }
        /// <summary>
        /// Gets a live reference to id's BeltComponent in factory.
        /// </summary>
        public static ref BeltComponent GetLive_BeltComponent(PlanetFactory factory, int poolId) {
            return ref factory.cargoTraffic.beltPool[poolId];
        }
        //no LINQ-helper GetLive because that doesn't work that way lol -- Eirshy
        /// <summary>
        /// Checks whether the component matches the entity.
        /// </summary>
        public static bool CheckEntity(in EntityData entity, in BeltComponent cmp) {
            return entity.id == cmp.entityId;
        }
        /// <summary>
        /// Checks whether the entity matches the component.
        /// </summary>
        public static bool CheckComponent(in EntityData entity, in BeltComponent cmp) {
            return entity.beltId == cmp.id;
        }
        /// <summary>
        /// Checks whether both the entity and the component match eachother.
        /// </summary>
        public static bool CheckCircular(in EntityData entity, in BeltComponent cmp) {
            return entity.id == cmp.entityId && entity.beltId == cmp.id;
        }

        #endregion
        #region Basic-Static SplitterComponent 
        /// <summary>
        /// True if the entity's splitterId does not point to the null SplitterComponent
        /// </summary>
        /// <remarks></remarks>
        public static bool Uses_SplitterComponent(in EntityData entity) => entity.splitterId != EntityData.Null.splitterId;
        /// <summary>
        /// True if the entity's splitterId does not point to the null SplitterComponent
        /// </summary>
        /// <remarks>Provided strictly for LINQ convenience; prefer the pass-by-ref version.</remarks>
        public static bool Uses_SplitterComponent(EntityData entity) => entity.splitterId != EntityData.Null.splitterId;
        /// <summary>
        /// True if the entity's SplitterComponent in the factory matches the entity.
        /// </summary>
        /// <remarks>
        /// Equivalent to calling <c>CheckEntity</c> on the result of <c>GetLive_SplitterComponent</c>
        /// <br />
        /// Provided first-class because this is the relationship the game itself checks (circa v0.9.24)
        /// </remarks>
        public static bool UsesActive_SplitterComponent(PlanetFactory factory, in EntityData entity) {
            return entity.id == factory.cargoTraffic.splitterPool[entity.splitterId].entityId;
        }
        /// <summary>
        /// Gets a live reference to entity's SplitterComponent in factory.
        /// </summary>
        public static ref SplitterComponent GetLive_SplitterComponent(PlanetFactory factory, in EntityData entity) {
            return ref factory.cargoTraffic.splitterPool[entity.splitterId];
        }
        /// <summary>
        /// Gets a live reference to id's SplitterComponent in factory.
        /// </summary>
        public static ref SplitterComponent GetLive_SplitterComponent(PlanetFactory factory, int poolId) {
            return ref factory.cargoTraffic.splitterPool[poolId];
        }
        //no LINQ-helper GetLive because that doesn't work that way lol -- Eirshy
        /// <summary>
        /// Checks whether the component matches the entity.
        /// </summary>
        public static bool CheckEntity(in EntityData entity, in SplitterComponent cmp) {
            return entity.id == cmp.entityId;
        }
        /// <summary>
        /// Checks whether the entity matches the component.
        /// </summary>
        public static bool CheckComponent(in EntityData entity, in SplitterComponent cmp) {
            return entity.splitterId == cmp.id;
        }
        /// <summary>
        /// Checks whether both the entity and the component match eachother.
        /// </summary>
        public static bool CheckCircular(in EntityData entity, in SplitterComponent cmp) {
            return entity.id == cmp.entityId && entity.splitterId == cmp.id;
        }

        #endregion
        #region Basic-Static MonitorComponent 
        /// <summary>
        /// True if the entity's monitorId does not point to the null MonitorComponent
        /// </summary>
        /// <remarks></remarks>
        public static bool Uses_MonitorComponent(in EntityData entity) => entity.monitorId != EntityData.Null.monitorId;
        /// <summary>
        /// True if the entity's monitorId does not point to the null MonitorComponent
        /// </summary>
        /// <remarks>Provided strictly for LINQ convenience; prefer the pass-by-ref version.</remarks>
        public static bool Uses_MonitorComponent(EntityData entity) => entity.monitorId != EntityData.Null.monitorId;
        /// <summary>
        /// True if the entity's MonitorComponent in the factory matches the entity.
        /// </summary>
        /// <remarks>
        /// Equivalent to calling <c>CheckEntity</c> on the result of <c>GetLive_MonitorComponent</c>
        /// <br />
        /// Provided first-class because this is the relationship the game itself checks (circa v0.9.24)
        /// </remarks>
        public static bool UsesActive_MonitorComponent(PlanetFactory factory, in EntityData entity) {
            return entity.id == factory.cargoTraffic.monitorPool[entity.monitorId].entityId;
        }
        /// <summary>
        /// Gets a live reference to entity's MonitorComponent in factory.
        /// </summary>
        public static ref MonitorComponent GetLive_MonitorComponent(PlanetFactory factory, in EntityData entity) {
            return ref factory.cargoTraffic.monitorPool[entity.monitorId];
        }
        /// <summary>
        /// Gets a live reference to id's MonitorComponent in factory.
        /// </summary>
        public static ref MonitorComponent GetLive_MonitorComponent(PlanetFactory factory, int poolId) {
            return ref factory.cargoTraffic.monitorPool[poolId];
        }
        //no LINQ-helper GetLive because that doesn't work that way lol -- Eirshy
        /// <summary>
        /// Checks whether the component matches the entity.
        /// </summary>
        public static bool CheckEntity(in EntityData entity, in MonitorComponent cmp) {
            return entity.id == cmp.entityId;
        }
        /// <summary>
        /// Checks whether the entity matches the component.
        /// </summary>
        public static bool CheckComponent(in EntityData entity, in MonitorComponent cmp) {
            return entity.monitorId == cmp.id;
        }
        /// <summary>
        /// Checks whether both the entity and the component match eachother.
        /// </summary>
        public static bool CheckCircular(in EntityData entity, in MonitorComponent cmp) {
            return entity.id == cmp.entityId && entity.monitorId == cmp.id;
        }

        #endregion
        #region Basic-Static SpraycoaterComponent 
        /// <summary>
        /// True if the entity's spraycoaterId does not point to the null SpraycoaterComponent
        /// </summary>
        /// <remarks></remarks>
        public static bool Uses_SpraycoaterComponent(in EntityData entity) => entity.spraycoaterId != EntityData.Null.spraycoaterId;
        /// <summary>
        /// True if the entity's spraycoaterId does not point to the null SpraycoaterComponent
        /// </summary>
        /// <remarks>Provided strictly for LINQ convenience; prefer the pass-by-ref version.</remarks>
        public static bool Uses_SpraycoaterComponent(EntityData entity) => entity.spraycoaterId != EntityData.Null.spraycoaterId;
        /// <summary>
        /// True if the entity's SpraycoaterComponent in the factory matches the entity.
        /// </summary>
        /// <remarks>
        /// Equivalent to calling <c>CheckEntity</c> on the result of <c>GetLive_SpraycoaterComponent</c>
        /// <br />
        /// Provided first-class because this is the relationship the game itself checks (circa v0.9.24)
        /// </remarks>
        public static bool UsesActive_SpraycoaterComponent(PlanetFactory factory, in EntityData entity) {
            return entity.id == factory.cargoTraffic.spraycoaterPool[entity.spraycoaterId].entityId;
        }
        /// <summary>
        /// Gets a live reference to entity's SpraycoaterComponent in factory.
        /// </summary>
        public static ref SpraycoaterComponent GetLive_SpraycoaterComponent(PlanetFactory factory, in EntityData entity) {
            return ref factory.cargoTraffic.spraycoaterPool[entity.spraycoaterId];
        }
        /// <summary>
        /// Gets a live reference to id's SpraycoaterComponent in factory.
        /// </summary>
        public static ref SpraycoaterComponent GetLive_SpraycoaterComponent(PlanetFactory factory, int poolId) {
            return ref factory.cargoTraffic.spraycoaterPool[poolId];
        }
        //no LINQ-helper GetLive because that doesn't work that way lol -- Eirshy
        /// <summary>
        /// Checks whether the component matches the entity.
        /// </summary>
        public static bool CheckEntity(in EntityData entity, in SpraycoaterComponent cmp) {
            return entity.id == cmp.entityId;
        }
        /// <summary>
        /// Checks whether the entity matches the component.
        /// </summary>
        public static bool CheckComponent(in EntityData entity, in SpraycoaterComponent cmp) {
            return entity.spraycoaterId == cmp.id;
        }
        /// <summary>
        /// Checks whether both the entity and the component match eachother.
        /// </summary>
        public static bool CheckCircular(in EntityData entity, in SpraycoaterComponent cmp) {
            return entity.id == cmp.entityId && entity.spraycoaterId == cmp.id;
        }

        #endregion
        #region Basic-Static PilerComponent 
        /// <summary>
        /// True if the entity's pilerId does not point to the null PilerComponent
        /// </summary>
        /// <remarks></remarks>
        public static bool Uses_PilerComponent(in EntityData entity) => entity.pilerId != EntityData.Null.pilerId;
        /// <summary>
        /// True if the entity's pilerId does not point to the null PilerComponent
        /// </summary>
        /// <remarks>Provided strictly for LINQ convenience; prefer the pass-by-ref version.</remarks>
        public static bool Uses_PilerComponent(EntityData entity) => entity.pilerId != EntityData.Null.pilerId;
        /// <summary>
        /// True if the entity's PilerComponent in the factory matches the entity.
        /// </summary>
        /// <remarks>
        /// Equivalent to calling <c>CheckEntity</c> on the result of <c>GetLive_PilerComponent</c>
        /// <br />
        /// Provided first-class because this is the relationship the game itself checks (circa v0.9.24)
        /// </remarks>
        public static bool UsesActive_PilerComponent(PlanetFactory factory, in EntityData entity) {
            return entity.id == factory.cargoTraffic.pilerPool[entity.pilerId].entityId;
        }
        /// <summary>
        /// Gets a live reference to entity's PilerComponent in factory.
        /// </summary>
        public static ref PilerComponent GetLive_PilerComponent(PlanetFactory factory, in EntityData entity) {
            return ref factory.cargoTraffic.pilerPool[entity.pilerId];
        }
        /// <summary>
        /// Gets a live reference to id's PilerComponent in factory.
        /// </summary>
        public static ref PilerComponent GetLive_PilerComponent(PlanetFactory factory, int poolId) {
            return ref factory.cargoTraffic.pilerPool[poolId];
        }
        //no LINQ-helper GetLive because that doesn't work that way lol -- Eirshy
        /// <summary>
        /// Checks whether the component matches the entity.
        /// </summary>
        public static bool CheckEntity(in EntityData entity, in PilerComponent cmp) {
            return entity.id == cmp.entityId;
        }
        /// <summary>
        /// Checks whether the entity matches the component.
        /// </summary>
        public static bool CheckComponent(in EntityData entity, in PilerComponent cmp) {
            return entity.pilerId == cmp.id;
        }
        /// <summary>
        /// Checks whether both the entity and the component match eachother.
        /// </summary>
        public static bool CheckCircular(in EntityData entity, in PilerComponent cmp) {
            return entity.id == cmp.entityId && entity.pilerId == cmp.id;
        }

        #endregion
        #region Basic-Static SpeakerComponent 
        /// <summary>
        /// True if the entity's speakerId does not point to the null SpeakerComponent
        /// </summary>
        /// <remarks></remarks>
        public static bool Uses_SpeakerComponent(in EntityData entity) => entity.speakerId != EntityData.Null.speakerId;
        /// <summary>
        /// True if the entity's speakerId does not point to the null SpeakerComponent
        /// </summary>
        /// <remarks>Provided strictly for LINQ convenience; prefer the pass-by-ref version.</remarks>
        public static bool Uses_SpeakerComponent(EntityData entity) => entity.speakerId != EntityData.Null.speakerId;
        /// <summary>
        /// True if the entity's SpeakerComponent in the factory matches the entity.
        /// </summary>
        /// <remarks>
        /// Equivalent to calling <c>CheckEntity</c> on the result of <c>GetLive_SpeakerComponent</c>
        /// <br />
        /// Provided first-class because this is the relationship the game itself checks (circa v0.9.24)
        /// </remarks>
        public static bool UsesActive_SpeakerComponent(PlanetFactory factory, in EntityData entity) {
            return entity.id == factory.digitalSystem.speakerPool[entity.speakerId].entityId;
        }
        /// <summary>
        /// Gets a live reference to entity's SpeakerComponent in factory.
        /// </summary>
        public static ref SpeakerComponent GetLive_SpeakerComponent(PlanetFactory factory, in EntityData entity) {
            return ref factory.digitalSystem.speakerPool[entity.speakerId];
        }
        /// <summary>
        /// Gets a live reference to id's SpeakerComponent in factory.
        /// </summary>
        public static ref SpeakerComponent GetLive_SpeakerComponent(PlanetFactory factory, int poolId) {
            return ref factory.digitalSystem.speakerPool[poolId];
        }
        //no LINQ-helper GetLive because that doesn't work that way lol -- Eirshy
        /// <summary>
        /// Checks whether the component matches the entity.
        /// </summary>
        public static bool CheckEntity(in EntityData entity, in SpeakerComponent cmp) {
            return entity.id == cmp.entityId;
        }
        /// <summary>
        /// Checks whether the entity matches the component.
        /// </summary>
        public static bool CheckComponent(in EntityData entity, in SpeakerComponent cmp) {
            return entity.speakerId == cmp.id;
        }
        /// <summary>
        /// Checks whether both the entity and the component match eachother.
        /// </summary>
        public static bool CheckCircular(in EntityData entity, in SpeakerComponent cmp) {
            return entity.id == cmp.entityId && entity.speakerId == cmp.id;
        }

        #endregion
        #region Basic-Static StorageComponent 
        /// <summary>
        /// True if the entity's storageId does not point to the null StorageComponent
        /// </summary>
        /// <remarks></remarks>
        public static bool Uses_StorageComponent(in EntityData entity) => entity.storageId != EntityData.Null.storageId;
        /// <summary>
        /// True if the entity's storageId does not point to the null StorageComponent
        /// </summary>
        /// <remarks>Provided strictly for LINQ convenience; prefer the pass-by-ref version.</remarks>
        public static bool Uses_StorageComponent(EntityData entity) => entity.storageId != EntityData.Null.storageId;
        /// <summary>
        /// True if the entity's StorageComponent in the factory matches the entity.
        /// </summary>
        /// <remarks>
        /// Equivalent to calling <c>CheckEntity</c> on the result of <c>GetLive_StorageComponent</c>
        /// <br />
        /// Provided first-class because this is the relationship the game itself checks (circa v0.9.24)
        /// </remarks>
        public static bool UsesActive_StorageComponent(PlanetFactory factory, in EntityData entity) {
            return entity.id == factory.factoryStorage.storagePool[entity.storageId].entityId;
        }
        /// <summary>
        /// Gets a live reference to entity's StorageComponent in factory.
        /// </summary>
        public static ref StorageComponent GetLive_StorageComponent(PlanetFactory factory, in EntityData entity) {
            return ref factory.factoryStorage.storagePool[entity.storageId];
        }
        /// <summary>
        /// Gets a live reference to id's StorageComponent in factory.
        /// </summary>
        public static ref StorageComponent GetLive_StorageComponent(PlanetFactory factory, int poolId) {
            return ref factory.factoryStorage.storagePool[poolId];
        }
        //no LINQ-helper GetLive because that doesn't work that way lol -- Eirshy
        /// <summary>
        /// Checks whether the component matches the entity.
        /// </summary>
        public static bool CheckEntity(in EntityData entity, in StorageComponent cmp) {
            return entity.id == cmp.entityId;
        }
        /// <summary>
        /// Checks whether the entity matches the component.
        /// </summary>
        public static bool CheckComponent(in EntityData entity, in StorageComponent cmp) {
            return entity.storageId == cmp.id;
        }
        /// <summary>
        /// Checks whether both the entity and the component match eachother.
        /// </summary>
        public static bool CheckCircular(in EntityData entity, in StorageComponent cmp) {
            return entity.id == cmp.entityId && entity.storageId == cmp.id;
        }

        #endregion
        #region Basic-Static TankComponent 
        /// <summary>
        /// True if the entity's tankId does not point to the null TankComponent
        /// </summary>
        /// <remarks></remarks>
        public static bool Uses_TankComponent(in EntityData entity) => entity.tankId != EntityData.Null.tankId;
        /// <summary>
        /// True if the entity's tankId does not point to the null TankComponent
        /// </summary>
        /// <remarks>Provided strictly for LINQ convenience; prefer the pass-by-ref version.</remarks>
        public static bool Uses_TankComponent(EntityData entity) => entity.tankId != EntityData.Null.tankId;
        /// <summary>
        /// True if the entity's TankComponent in the factory matches the entity.
        /// </summary>
        /// <remarks>
        /// Equivalent to calling <c>CheckEntity</c> on the result of <c>GetLive_TankComponent</c>
        /// <br />
        /// Provided first-class because this is the relationship the game itself checks (circa v0.9.24)
        /// </remarks>
        public static bool UsesActive_TankComponent(PlanetFactory factory, in EntityData entity) {
            return entity.id == factory.factoryStorage.tankPool[entity.tankId].entityId;
        }
        /// <summary>
        /// Gets a live reference to entity's TankComponent in factory.
        /// </summary>
        public static ref TankComponent GetLive_TankComponent(PlanetFactory factory, in EntityData entity) {
            return ref factory.factoryStorage.tankPool[entity.tankId];
        }
        /// <summary>
        /// Gets a live reference to id's TankComponent in factory.
        /// </summary>
        public static ref TankComponent GetLive_TankComponent(PlanetFactory factory, int poolId) {
            return ref factory.factoryStorage.tankPool[poolId];
        }
        //no LINQ-helper GetLive because that doesn't work that way lol -- Eirshy
        /// <summary>
        /// Checks whether the component matches the entity.
        /// </summary>
        public static bool CheckEntity(in EntityData entity, in TankComponent cmp) {
            return entity.id == cmp.entityId;
        }
        /// <summary>
        /// Checks whether the entity matches the component.
        /// </summary>
        public static bool CheckComponent(in EntityData entity, in TankComponent cmp) {
            return entity.tankId == cmp.id;
        }
        /// <summary>
        /// Checks whether both the entity and the component match eachother.
        /// </summary>
        public static bool CheckCircular(in EntityData entity, in TankComponent cmp) {
            return entity.id == cmp.entityId && entity.tankId == cmp.id;
        }

        #endregion
        #region Basic-Static MinerComponent 
        /// <summary>
        /// True if the entity's minerId does not point to the null MinerComponent
        /// </summary>
        /// <remarks></remarks>
        public static bool Uses_MinerComponent(in EntityData entity) => entity.minerId != EntityData.Null.minerId;
        /// <summary>
        /// True if the entity's minerId does not point to the null MinerComponent
        /// </summary>
        /// <remarks>Provided strictly for LINQ convenience; prefer the pass-by-ref version.</remarks>
        public static bool Uses_MinerComponent(EntityData entity) => entity.minerId != EntityData.Null.minerId;
        /// <summary>
        /// True if the entity's MinerComponent in the factory matches the entity.
        /// </summary>
        /// <remarks>
        /// Equivalent to calling <c>CheckEntity</c> on the result of <c>GetLive_MinerComponent</c>
        /// <br />
        /// Provided first-class because this is the relationship the game itself checks (circa v0.9.24)
        /// </remarks>
        public static bool UsesActive_MinerComponent(PlanetFactory factory, in EntityData entity) {
            return entity.id == factory.factorySystem.minerPool[entity.minerId].entityId;
        }
        /// <summary>
        /// Gets a live reference to entity's MinerComponent in factory.
        /// </summary>
        public static ref MinerComponent GetLive_MinerComponent(PlanetFactory factory, in EntityData entity) {
            return ref factory.factorySystem.minerPool[entity.minerId];
        }
        /// <summary>
        /// Gets a live reference to id's MinerComponent in factory.
        /// </summary>
        public static ref MinerComponent GetLive_MinerComponent(PlanetFactory factory, int poolId) {
            return ref factory.factorySystem.minerPool[poolId];
        }
        //no LINQ-helper GetLive because that doesn't work that way lol -- Eirshy
        /// <summary>
        /// Checks whether the component matches the entity.
        /// </summary>
        public static bool CheckEntity(in EntityData entity, in MinerComponent cmp) {
            return entity.id == cmp.entityId;
        }
        /// <summary>
        /// Checks whether the entity matches the component.
        /// </summary>
        public static bool CheckComponent(in EntityData entity, in MinerComponent cmp) {
            return entity.minerId == cmp.id;
        }
        /// <summary>
        /// Checks whether both the entity and the component match eachother.
        /// </summary>
        public static bool CheckCircular(in EntityData entity, in MinerComponent cmp) {
            return entity.id == cmp.entityId && entity.minerId == cmp.id;
        }

        #endregion
        #region Basic-Static InserterComponent 
        /// <summary>
        /// True if the entity's inserterId does not point to the null InserterComponent
        /// </summary>
        /// <remarks></remarks>
        public static bool Uses_InserterComponent(in EntityData entity) => entity.inserterId != EntityData.Null.inserterId;
        /// <summary>
        /// True if the entity's inserterId does not point to the null InserterComponent
        /// </summary>
        /// <remarks>Provided strictly for LINQ convenience; prefer the pass-by-ref version.</remarks>
        public static bool Uses_InserterComponent(EntityData entity) => entity.inserterId != EntityData.Null.inserterId;
        /// <summary>
        /// True if the entity's InserterComponent in the factory matches the entity.
        /// </summary>
        /// <remarks>
        /// Equivalent to calling <c>CheckEntity</c> on the result of <c>GetLive_InserterComponent</c>
        /// <br />
        /// Provided first-class because this is the relationship the game itself checks (circa v0.9.24)
        /// </remarks>
        public static bool UsesActive_InserterComponent(PlanetFactory factory, in EntityData entity) {
            return entity.id == factory.factorySystem.inserterPool[entity.inserterId].entityId;
        }
        /// <summary>
        /// Gets a live reference to entity's InserterComponent in factory.
        /// </summary>
        public static ref InserterComponent GetLive_InserterComponent(PlanetFactory factory, in EntityData entity) {
            return ref factory.factorySystem.inserterPool[entity.inserterId];
        }
        /// <summary>
        /// Gets a live reference to id's InserterComponent in factory.
        /// </summary>
        public static ref InserterComponent GetLive_InserterComponent(PlanetFactory factory, int poolId) {
            return ref factory.factorySystem.inserterPool[poolId];
        }
        //no LINQ-helper GetLive because that doesn't work that way lol -- Eirshy
        /// <summary>
        /// Checks whether the component matches the entity.
        /// </summary>
        public static bool CheckEntity(in EntityData entity, in InserterComponent cmp) {
            return entity.id == cmp.entityId;
        }
        /// <summary>
        /// Checks whether the entity matches the component.
        /// </summary>
        public static bool CheckComponent(in EntityData entity, in InserterComponent cmp) {
            return entity.inserterId == cmp.id;
        }
        /// <summary>
        /// Checks whether both the entity and the component match eachother.
        /// </summary>
        public static bool CheckCircular(in EntityData entity, in InserterComponent cmp) {
            return entity.id == cmp.entityId && entity.inserterId == cmp.id;
        }

        #endregion
        #region Basic-Static AssemblerComponent 
        /// <summary>
        /// True if the entity's assemblerId does not point to the null AssemblerComponent
        /// </summary>
        /// <remarks></remarks>
        public static bool Uses_AssemblerComponent(in EntityData entity) => entity.assemblerId != EntityData.Null.assemblerId;
        /// <summary>
        /// True if the entity's assemblerId does not point to the null AssemblerComponent
        /// </summary>
        /// <remarks>Provided strictly for LINQ convenience; prefer the pass-by-ref version.</remarks>
        public static bool Uses_AssemblerComponent(EntityData entity) => entity.assemblerId != EntityData.Null.assemblerId;
        /// <summary>
        /// True if the entity's AssemblerComponent in the factory matches the entity.
        /// </summary>
        /// <remarks>
        /// Equivalent to calling <c>CheckEntity</c> on the result of <c>GetLive_AssemblerComponent</c>
        /// <br />
        /// Provided first-class because this is the relationship the game itself checks (circa v0.9.24)
        /// </remarks>
        public static bool UsesActive_AssemblerComponent(PlanetFactory factory, in EntityData entity) {
            return entity.id == factory.factorySystem.assemblerPool[entity.assemblerId].entityId;
        }
        /// <summary>
        /// Gets a live reference to entity's AssemblerComponent in factory.
        /// </summary>
        public static ref AssemblerComponent GetLive_AssemblerComponent(PlanetFactory factory, in EntityData entity) {
            return ref factory.factorySystem.assemblerPool[entity.assemblerId];
        }
        /// <summary>
        /// Gets a live reference to id's AssemblerComponent in factory.
        /// </summary>
        public static ref AssemblerComponent GetLive_AssemblerComponent(PlanetFactory factory, int poolId) {
            return ref factory.factorySystem.assemblerPool[poolId];
        }
        //no LINQ-helper GetLive because that doesn't work that way lol -- Eirshy
        /// <summary>
        /// Checks whether the component matches the entity.
        /// </summary>
        public static bool CheckEntity(in EntityData entity, in AssemblerComponent cmp) {
            return entity.id == cmp.entityId;
        }
        /// <summary>
        /// Checks whether the entity matches the component.
        /// </summary>
        public static bool CheckComponent(in EntityData entity, in AssemblerComponent cmp) {
            return entity.assemblerId == cmp.id;
        }
        /// <summary>
        /// Checks whether both the entity and the component match eachother.
        /// </summary>
        public static bool CheckCircular(in EntityData entity, in AssemblerComponent cmp) {
            return entity.id == cmp.entityId && entity.assemblerId == cmp.id;
        }

        #endregion
        #region Basic-Static FractionatorComponent 
        /// <summary>
        /// True if the entity's fractionatorId does not point to the null FractionatorComponent
        /// </summary>
        /// <remarks></remarks>
        public static bool Uses_FractionatorComponent(in EntityData entity) => entity.fractionatorId != EntityData.Null.fractionatorId;
        /// <summary>
        /// True if the entity's fractionatorId does not point to the null FractionatorComponent
        /// </summary>
        /// <remarks>Provided strictly for LINQ convenience; prefer the pass-by-ref version.</remarks>
        public static bool Uses_FractionatorComponent(EntityData entity) => entity.fractionatorId != EntityData.Null.fractionatorId;
        /// <summary>
        /// True if the entity's FractionatorComponent in the factory matches the entity.
        /// </summary>
        /// <remarks>
        /// Equivalent to calling <c>CheckEntity</c> on the result of <c>GetLive_FractionatorComponent</c>
        /// <br />
        /// Provided first-class because this is the relationship the game itself checks (circa v0.9.24)
        /// </remarks>
        public static bool UsesActive_FractionatorComponent(PlanetFactory factory, in EntityData entity) {
            return entity.id == factory.factorySystem.fractionatorPool[entity.fractionatorId].entityId;
        }
        /// <summary>
        /// Gets a live reference to entity's FractionatorComponent in factory.
        /// </summary>
        public static ref FractionatorComponent GetLive_FractionatorComponent(PlanetFactory factory, in EntityData entity) {
            return ref factory.factorySystem.fractionatorPool[entity.fractionatorId];
        }
        /// <summary>
        /// Gets a live reference to id's FractionatorComponent in factory.
        /// </summary>
        public static ref FractionatorComponent GetLive_FractionatorComponent(PlanetFactory factory, int poolId) {
            return ref factory.factorySystem.fractionatorPool[poolId];
        }
        //no LINQ-helper GetLive because that doesn't work that way lol -- Eirshy
        /// <summary>
        /// Checks whether the component matches the entity.
        /// </summary>
        public static bool CheckEntity(in EntityData entity, in FractionatorComponent cmp) {
            return entity.id == cmp.entityId;
        }
        /// <summary>
        /// Checks whether the entity matches the component.
        /// </summary>
        public static bool CheckComponent(in EntityData entity, in FractionatorComponent cmp) {
            return entity.fractionatorId == cmp.id;
        }
        /// <summary>
        /// Checks whether both the entity and the component match eachother.
        /// </summary>
        public static bool CheckCircular(in EntityData entity, in FractionatorComponent cmp) {
            return entity.id == cmp.entityId && entity.fractionatorId == cmp.id;
        }

        #endregion
        #region Basic-Static EjectorComponent 
        /// <summary>
        /// True if the entity's ejectorId does not point to the null EjectorComponent
        /// </summary>
        /// <remarks></remarks>
        public static bool Uses_EjectorComponent(in EntityData entity) => entity.ejectorId != EntityData.Null.ejectorId;
        /// <summary>
        /// True if the entity's ejectorId does not point to the null EjectorComponent
        /// </summary>
        /// <remarks>Provided strictly for LINQ convenience; prefer the pass-by-ref version.</remarks>
        public static bool Uses_EjectorComponent(EntityData entity) => entity.ejectorId != EntityData.Null.ejectorId;
        /// <summary>
        /// True if the entity's EjectorComponent in the factory matches the entity.
        /// </summary>
        /// <remarks>
        /// Equivalent to calling <c>CheckEntity</c> on the result of <c>GetLive_EjectorComponent</c>
        /// <br />
        /// Provided first-class because this is the relationship the game itself checks (circa v0.9.24)
        /// </remarks>
        public static bool UsesActive_EjectorComponent(PlanetFactory factory, in EntityData entity) {
            return entity.id == factory.factorySystem.ejectorPool[entity.ejectorId].entityId;
        }
        /// <summary>
        /// Gets a live reference to entity's EjectorComponent in factory.
        /// </summary>
        public static ref EjectorComponent GetLive_EjectorComponent(PlanetFactory factory, in EntityData entity) {
            return ref factory.factorySystem.ejectorPool[entity.ejectorId];
        }
        /// <summary>
        /// Gets a live reference to id's EjectorComponent in factory.
        /// </summary>
        public static ref EjectorComponent GetLive_EjectorComponent(PlanetFactory factory, int poolId) {
            return ref factory.factorySystem.ejectorPool[poolId];
        }
        //no LINQ-helper GetLive because that doesn't work that way lol -- Eirshy
        /// <summary>
        /// Checks whether the component matches the entity.
        /// </summary>
        public static bool CheckEntity(in EntityData entity, in EjectorComponent cmp) {
            return entity.id == cmp.entityId;
        }
        /// <summary>
        /// Checks whether the entity matches the component.
        /// </summary>
        public static bool CheckComponent(in EntityData entity, in EjectorComponent cmp) {
            return entity.ejectorId == cmp.id;
        }
        /// <summary>
        /// Checks whether both the entity and the component match eachother.
        /// </summary>
        public static bool CheckCircular(in EntityData entity, in EjectorComponent cmp) {
            return entity.id == cmp.entityId && entity.ejectorId == cmp.id;
        }

        #endregion
        #region Basic-Static SiloComponent 
        /// <summary>
        /// True if the entity's siloId does not point to the null SiloComponent
        /// </summary>
        /// <remarks></remarks>
        public static bool Uses_SiloComponent(in EntityData entity) => entity.siloId != EntityData.Null.siloId;
        /// <summary>
        /// True if the entity's siloId does not point to the null SiloComponent
        /// </summary>
        /// <remarks>Provided strictly for LINQ convenience; prefer the pass-by-ref version.</remarks>
        public static bool Uses_SiloComponent(EntityData entity) => entity.siloId != EntityData.Null.siloId;
        /// <summary>
        /// True if the entity's SiloComponent in the factory matches the entity.
        /// </summary>
        /// <remarks>
        /// Equivalent to calling <c>CheckEntity</c> on the result of <c>GetLive_SiloComponent</c>
        /// <br />
        /// Provided first-class because this is the relationship the game itself checks (circa v0.9.24)
        /// </remarks>
        public static bool UsesActive_SiloComponent(PlanetFactory factory, in EntityData entity) {
            return entity.id == factory.factorySystem.siloPool[entity.siloId].entityId;
        }
        /// <summary>
        /// Gets a live reference to entity's SiloComponent in factory.
        /// </summary>
        public static ref SiloComponent GetLive_SiloComponent(PlanetFactory factory, in EntityData entity) {
            return ref factory.factorySystem.siloPool[entity.siloId];
        }
        /// <summary>
        /// Gets a live reference to id's SiloComponent in factory.
        /// </summary>
        public static ref SiloComponent GetLive_SiloComponent(PlanetFactory factory, int poolId) {
            return ref factory.factorySystem.siloPool[poolId];
        }
        //no LINQ-helper GetLive because that doesn't work that way lol -- Eirshy
        /// <summary>
        /// Checks whether the component matches the entity.
        /// </summary>
        public static bool CheckEntity(in EntityData entity, in SiloComponent cmp) {
            return entity.id == cmp.entityId;
        }
        /// <summary>
        /// Checks whether the entity matches the component.
        /// </summary>
        public static bool CheckComponent(in EntityData entity, in SiloComponent cmp) {
            return entity.siloId == cmp.id;
        }
        /// <summary>
        /// Checks whether both the entity and the component match eachother.
        /// </summary>
        public static bool CheckCircular(in EntityData entity, in SiloComponent cmp) {
            return entity.id == cmp.entityId && entity.siloId == cmp.id;
        }

        #endregion
        #region Basic-Static LabComponent 
        /// <summary>
        /// True if the entity's labId does not point to the null LabComponent
        /// </summary>
        /// <remarks></remarks>
        public static bool Uses_LabComponent(in EntityData entity) => entity.labId != EntityData.Null.labId;
        /// <summary>
        /// True if the entity's labId does not point to the null LabComponent
        /// </summary>
        /// <remarks>Provided strictly for LINQ convenience; prefer the pass-by-ref version.</remarks>
        public static bool Uses_LabComponent(EntityData entity) => entity.labId != EntityData.Null.labId;
        /// <summary>
        /// True if the entity's LabComponent in the factory matches the entity.
        /// </summary>
        /// <remarks>
        /// Equivalent to calling <c>CheckEntity</c> on the result of <c>GetLive_LabComponent</c>
        /// <br />
        /// Provided first-class because this is the relationship the game itself checks (circa v0.9.24)
        /// </remarks>
        public static bool UsesActive_LabComponent(PlanetFactory factory, in EntityData entity) {
            return entity.id == factory.factorySystem.labPool[entity.labId].entityId;
        }
        /// <summary>
        /// Gets a live reference to entity's LabComponent in factory.
        /// </summary>
        public static ref LabComponent GetLive_LabComponent(PlanetFactory factory, in EntityData entity) {
            return ref factory.factorySystem.labPool[entity.labId];
        }
        /// <summary>
        /// Gets a live reference to id's LabComponent in factory.
        /// </summary>
        public static ref LabComponent GetLive_LabComponent(PlanetFactory factory, int poolId) {
            return ref factory.factorySystem.labPool[poolId];
        }
        //no LINQ-helper GetLive because that doesn't work that way lol -- Eirshy
        /// <summary>
        /// Checks whether the component matches the entity.
        /// </summary>
        public static bool CheckEntity(in EntityData entity, in LabComponent cmp) {
            return entity.id == cmp.entityId;
        }
        /// <summary>
        /// Checks whether the entity matches the component.
        /// </summary>
        public static bool CheckComponent(in EntityData entity, in LabComponent cmp) {
            return entity.labId == cmp.id;
        }
        /// <summary>
        /// Checks whether both the entity and the component match eachother.
        /// </summary>
        public static bool CheckCircular(in EntityData entity, in LabComponent cmp) {
            return entity.id == cmp.entityId && entity.labId == cmp.id;
        }

        #endregion
        #region Basic-Static StationComponent 
        /// <summary>
        /// True if the entity's stationId does not point to the null StationComponent
        /// </summary>
        /// <remarks></remarks>
        public static bool Uses_StationComponent(in EntityData entity) => entity.stationId != EntityData.Null.stationId;
        /// <summary>
        /// True if the entity's stationId does not point to the null StationComponent
        /// </summary>
        /// <remarks>Provided strictly for LINQ convenience; prefer the pass-by-ref version.</remarks>
        public static bool Uses_StationComponent(EntityData entity) => entity.stationId != EntityData.Null.stationId;
        /// <summary>
        /// True if the entity's StationComponent in the factory matches the entity.
        /// </summary>
        /// <remarks>
        /// Equivalent to calling <c>CheckEntity</c> on the result of <c>GetLive_StationComponent</c>
        /// <br />
        /// Provided first-class because this is the relationship the game itself checks (circa v0.9.24)
        /// </remarks>
        public static bool UsesActive_StationComponent(PlanetFactory factory, in EntityData entity) {
            return entity.id == factory.transport.stationPool[entity.stationId].entityId;
        }
        /// <summary>
        /// Gets a live reference to entity's StationComponent in factory.
        /// </summary>
        public static ref StationComponent GetLive_StationComponent(PlanetFactory factory, in EntityData entity) {
            return ref factory.transport.stationPool[entity.stationId];
        }
        /// <summary>
        /// Gets a live reference to id's StationComponent in factory.
        /// </summary>
        public static ref StationComponent GetLive_StationComponent(PlanetFactory factory, int poolId) {
            return ref factory.transport.stationPool[poolId];
        }
        //no LINQ-helper GetLive because that doesn't work that way lol -- Eirshy
        /// <summary>
        /// Checks whether the component matches the entity.
        /// </summary>
        public static bool CheckEntity(in EntityData entity, in StationComponent cmp) {
            return entity.id == cmp.entityId;
        }
        /// <summary>
        /// Checks whether the entity matches the component.
        /// </summary>
        public static bool CheckComponent(in EntityData entity, in StationComponent cmp) {
            return entity.stationId == cmp.id;
        }
        /// <summary>
        /// Checks whether both the entity and the component match eachother.
        /// </summary>
        public static bool CheckCircular(in EntityData entity, in StationComponent cmp) {
            return entity.id == cmp.entityId && entity.stationId == cmp.id;
        }

        #endregion
        #region Basic-Static PowerGeneratorComponent 
        /// <summary>
        /// True if the entity's powerGenId does not point to the null PowerGeneratorComponent
        /// </summary>
        /// <remarks></remarks>
        public static bool Uses_PowerGeneratorComponent(in EntityData entity) => entity.powerGenId != EntityData.Null.powerGenId;
        /// <summary>
        /// True if the entity's powerGenId does not point to the null PowerGeneratorComponent
        /// </summary>
        /// <remarks>Provided strictly for LINQ convenience; prefer the pass-by-ref version.</remarks>
        public static bool Uses_PowerGeneratorComponent(EntityData entity) => entity.powerGenId != EntityData.Null.powerGenId;
        /// <summary>
        /// True if the entity's PowerGeneratorComponent in the factory matches the entity.
        /// </summary>
        /// <remarks>
        /// Equivalent to calling <c>CheckEntity</c> on the result of <c>GetLive_PowerGeneratorComponent</c>
        /// <br />
        /// Provided first-class because this is the relationship the game itself checks (circa v0.9.24)
        /// </remarks>
        public static bool UsesActive_PowerGeneratorComponent(PlanetFactory factory, in EntityData entity) {
            return entity.id == factory.powerSystem.genPool[entity.powerGenId].entityId;
        }
        /// <summary>
        /// Gets a live reference to entity's PowerGeneratorComponent in factory.
        /// </summary>
        public static ref PowerGeneratorComponent GetLive_PowerGeneratorComponent(PlanetFactory factory, in EntityData entity) {
            return ref factory.powerSystem.genPool[entity.powerGenId];
        }
        /// <summary>
        /// Gets a live reference to id's PowerGeneratorComponent in factory.
        /// </summary>
        public static ref PowerGeneratorComponent GetLive_PowerGeneratorComponent(PlanetFactory factory, int poolId) {
            return ref factory.powerSystem.genPool[poolId];
        }
        //no LINQ-helper GetLive because that doesn't work that way lol -- Eirshy
        /// <summary>
        /// Checks whether the component matches the entity.
        /// </summary>
        public static bool CheckEntity(in EntityData entity, in PowerGeneratorComponent cmp) {
            return entity.id == cmp.entityId;
        }
        /// <summary>
        /// Checks whether the entity matches the component.
        /// </summary>
        public static bool CheckComponent(in EntityData entity, in PowerGeneratorComponent cmp) {
            return entity.powerGenId == cmp.id;
        }
        /// <summary>
        /// Checks whether both the entity and the component match eachother.
        /// </summary>
        public static bool CheckCircular(in EntityData entity, in PowerGeneratorComponent cmp) {
            return entity.id == cmp.entityId && entity.powerGenId == cmp.id;
        }

        #endregion
        #region Basic-Static PowerNodeComponent 
        /// <summary>
        /// True if the entity's powerNodeId does not point to the null PowerNodeComponent
        /// </summary>
        /// <remarks></remarks>
        public static bool Uses_PowerNodeComponent(in EntityData entity) => entity.powerNodeId != EntityData.Null.powerNodeId;
        /// <summary>
        /// True if the entity's powerNodeId does not point to the null PowerNodeComponent
        /// </summary>
        /// <remarks>Provided strictly for LINQ convenience; prefer the pass-by-ref version.</remarks>
        public static bool Uses_PowerNodeComponent(EntityData entity) => entity.powerNodeId != EntityData.Null.powerNodeId;
        /// <summary>
        /// True if the entity's PowerNodeComponent in the factory matches the entity.
        /// </summary>
        /// <remarks>
        /// Equivalent to calling <c>CheckEntity</c> on the result of <c>GetLive_PowerNodeComponent</c>
        /// <br />
        /// Provided first-class because this is the relationship the game itself checks (circa v0.9.24)
        /// </remarks>
        public static bool UsesActive_PowerNodeComponent(PlanetFactory factory, in EntityData entity) {
            return entity.id == factory.powerSystem.nodePool[entity.powerNodeId].entityId;
        }
        /// <summary>
        /// Gets a live reference to entity's PowerNodeComponent in factory.
        /// </summary>
        public static ref PowerNodeComponent GetLive_PowerNodeComponent(PlanetFactory factory, in EntityData entity) {
            return ref factory.powerSystem.nodePool[entity.powerNodeId];
        }
        /// <summary>
        /// Gets a live reference to id's PowerNodeComponent in factory.
        /// </summary>
        public static ref PowerNodeComponent GetLive_PowerNodeComponent(PlanetFactory factory, int poolId) {
            return ref factory.powerSystem.nodePool[poolId];
        }
        //no LINQ-helper GetLive because that doesn't work that way lol -- Eirshy
        /// <summary>
        /// Checks whether the component matches the entity.
        /// </summary>
        public static bool CheckEntity(in EntityData entity, in PowerNodeComponent cmp) {
            return entity.id == cmp.entityId;
        }
        /// <summary>
        /// Checks whether the entity matches the component.
        /// </summary>
        public static bool CheckComponent(in EntityData entity, in PowerNodeComponent cmp) {
            return entity.powerNodeId == cmp.id;
        }
        /// <summary>
        /// Checks whether both the entity and the component match eachother.
        /// </summary>
        public static bool CheckCircular(in EntityData entity, in PowerNodeComponent cmp) {
            return entity.id == cmp.entityId && entity.powerNodeId == cmp.id;
        }

        #endregion
        #region Basic-Static PowerConsumerComponent 
        /// <summary>
        /// True if the entity's powerConId does not point to the null PowerConsumerComponent
        /// </summary>
        /// <remarks></remarks>
        public static bool Uses_PowerConsumerComponent(in EntityData entity) => entity.powerConId != EntityData.Null.powerConId;
        /// <summary>
        /// True if the entity's powerConId does not point to the null PowerConsumerComponent
        /// </summary>
        /// <remarks>Provided strictly for LINQ convenience; prefer the pass-by-ref version.</remarks>
        public static bool Uses_PowerConsumerComponent(EntityData entity) => entity.powerConId != EntityData.Null.powerConId;
        /// <summary>
        /// True if the entity's PowerConsumerComponent in the factory matches the entity.
        /// </summary>
        /// <remarks>
        /// Equivalent to calling <c>CheckEntity</c> on the result of <c>GetLive_PowerConsumerComponent</c>
        /// <br />
        /// Provided first-class because this is the relationship the game itself checks (circa v0.9.24)
        /// </remarks>
        public static bool UsesActive_PowerConsumerComponent(PlanetFactory factory, in EntityData entity) {
            return entity.id == factory.powerSystem.consumerPool[entity.powerConId].entityId;
        }
        /// <summary>
        /// Gets a live reference to entity's PowerConsumerComponent in factory.
        /// </summary>
        public static ref PowerConsumerComponent GetLive_PowerConsumerComponent(PlanetFactory factory, in EntityData entity) {
            return ref factory.powerSystem.consumerPool[entity.powerConId];
        }
        /// <summary>
        /// Gets a live reference to id's PowerConsumerComponent in factory.
        /// </summary>
        public static ref PowerConsumerComponent GetLive_PowerConsumerComponent(PlanetFactory factory, int poolId) {
            return ref factory.powerSystem.consumerPool[poolId];
        }
        //no LINQ-helper GetLive because that doesn't work that way lol -- Eirshy
        /// <summary>
        /// Checks whether the component matches the entity.
        /// </summary>
        public static bool CheckEntity(in EntityData entity, in PowerConsumerComponent cmp) {
            return entity.id == cmp.entityId;
        }
        /// <summary>
        /// Checks whether the entity matches the component.
        /// </summary>
        public static bool CheckComponent(in EntityData entity, in PowerConsumerComponent cmp) {
            return entity.powerConId == cmp.id;
        }
        /// <summary>
        /// Checks whether both the entity and the component match eachother.
        /// </summary>
        public static bool CheckCircular(in EntityData entity, in PowerConsumerComponent cmp) {
            return entity.id == cmp.entityId && entity.powerConId == cmp.id;
        }

        #endregion
        #region Basic-Static PowerAccumulatorComponent 
        /// <summary>
        /// True if the entity's powerAccId does not point to the null PowerAccumulatorComponent
        /// </summary>
        /// <remarks></remarks>
        public static bool Uses_PowerAccumulatorComponent(in EntityData entity) => entity.powerAccId != EntityData.Null.powerAccId;
        /// <summary>
        /// True if the entity's powerAccId does not point to the null PowerAccumulatorComponent
        /// </summary>
        /// <remarks>Provided strictly for LINQ convenience; prefer the pass-by-ref version.</remarks>
        public static bool Uses_PowerAccumulatorComponent(EntityData entity) => entity.powerAccId != EntityData.Null.powerAccId;
        /// <summary>
        /// True if the entity's PowerAccumulatorComponent in the factory matches the entity.
        /// </summary>
        /// <remarks>
        /// Equivalent to calling <c>CheckEntity</c> on the result of <c>GetLive_PowerAccumulatorComponent</c>
        /// <br />
        /// Provided first-class because this is the relationship the game itself checks (circa v0.9.24)
        /// </remarks>
        public static bool UsesActive_PowerAccumulatorComponent(PlanetFactory factory, in EntityData entity) {
            return entity.id == factory.powerSystem.accPool[entity.powerAccId].entityId;
        }
        /// <summary>
        /// Gets a live reference to entity's PowerAccumulatorComponent in factory.
        /// </summary>
        public static ref PowerAccumulatorComponent GetLive_PowerAccumulatorComponent(PlanetFactory factory, in EntityData entity) {
            return ref factory.powerSystem.accPool[entity.powerAccId];
        }
        /// <summary>
        /// Gets a live reference to id's PowerAccumulatorComponent in factory.
        /// </summary>
        public static ref PowerAccumulatorComponent GetLive_PowerAccumulatorComponent(PlanetFactory factory, int poolId) {
            return ref factory.powerSystem.accPool[poolId];
        }
        //no LINQ-helper GetLive because that doesn't work that way lol -- Eirshy
        /// <summary>
        /// Checks whether the component matches the entity.
        /// </summary>
        public static bool CheckEntity(in EntityData entity, in PowerAccumulatorComponent cmp) {
            return entity.id == cmp.entityId;
        }
        /// <summary>
        /// Checks whether the entity matches the component.
        /// </summary>
        public static bool CheckComponent(in EntityData entity, in PowerAccumulatorComponent cmp) {
            return entity.powerAccId == cmp.id;
        }
        /// <summary>
        /// Checks whether both the entity and the component match eachother.
        /// </summary>
        public static bool CheckCircular(in EntityData entity, in PowerAccumulatorComponent cmp) {
            return entity.id == cmp.entityId && entity.powerAccId == cmp.id;
        }

        #endregion
        #region Basic-Static PowerExchangerComponent 
        /// <summary>
        /// True if the entity's powerExcId does not point to the null PowerExchangerComponent
        /// </summary>
        /// <remarks></remarks>
        public static bool Uses_PowerExchangerComponent(in EntityData entity) => entity.powerExcId != EntityData.Null.powerExcId;
        /// <summary>
        /// True if the entity's powerExcId does not point to the null PowerExchangerComponent
        /// </summary>
        /// <remarks>Provided strictly for LINQ convenience; prefer the pass-by-ref version.</remarks>
        public static bool Uses_PowerExchangerComponent(EntityData entity) => entity.powerExcId != EntityData.Null.powerExcId;
        /// <summary>
        /// True if the entity's PowerExchangerComponent in the factory matches the entity.
        /// </summary>
        /// <remarks>
        /// Equivalent to calling <c>CheckEntity</c> on the result of <c>GetLive_PowerExchangerComponent</c>
        /// <br />
        /// Provided first-class because this is the relationship the game itself checks (circa v0.9.24)
        /// </remarks>
        public static bool UsesActive_PowerExchangerComponent(PlanetFactory factory, in EntityData entity) {
            return entity.id == factory.powerSystem.excPool[entity.powerExcId].entityId;
        }
        /// <summary>
        /// Gets a live reference to entity's PowerExchangerComponent in factory.
        /// </summary>
        public static ref PowerExchangerComponent GetLive_PowerExchangerComponent(PlanetFactory factory, in EntityData entity) {
            return ref factory.powerSystem.excPool[entity.powerExcId];
        }
        /// <summary>
        /// Gets a live reference to id's PowerExchangerComponent in factory.
        /// </summary>
        public static ref PowerExchangerComponent GetLive_PowerExchangerComponent(PlanetFactory factory, int poolId) {
            return ref factory.powerSystem.excPool[poolId];
        }
        //no LINQ-helper GetLive because that doesn't work that way lol -- Eirshy
        /// <summary>
        /// Checks whether the component matches the entity.
        /// </summary>
        public static bool CheckEntity(in EntityData entity, in PowerExchangerComponent cmp) {
            return entity.id == cmp.entityId;
        }
        /// <summary>
        /// Checks whether the entity matches the component.
        /// </summary>
        public static bool CheckComponent(in EntityData entity, in PowerExchangerComponent cmp) {
            return entity.powerExcId == cmp.id;
        }
        /// <summary>
        /// Checks whether both the entity and the component match eachother.
        /// </summary>
        public static bool CheckCircular(in EntityData entity, in PowerExchangerComponent cmp) {
            return entity.id == cmp.entityId && entity.powerExcId == cmp.id;
        }

        #endregion

        //Basic-LINQ -- Uses_, UsesActive_ (LINQ helper static versions)
        #region Basic-LINQ BeltComponent

        /// <summary>
        /// True if the entity's beltId does not point to the null BeltComponent
        /// </summary>
        /// <remarks>Provided as a LINQ helper.</remarks>
        public static bool Uses_BeltComponent(EntityRef entr) => entr.Has_BeltComponent;
        /// <summary>
        /// True if the entity's BeltComponent in the factory matches the entity.
        /// </summary>
        /// <remarks>Provided as a LINQ helper.</remarks>
        public static bool UsesActive_BeltComponent(EntityRef entr) => entr.HasActive_BeltComponent;

        #endregion
        #region Basic-LINQ SplitterComponent

        /// <summary>
        /// True if the entity's splitterId does not point to the null SplitterComponent
        /// </summary>
        /// <remarks>Provided as a LINQ helper.</remarks>
        public static bool Uses_SplitterComponent(EntityRef entr) => entr.Has_SplitterComponent;
        /// <summary>
        /// True if the entity's SplitterComponent in the factory matches the entity.
        /// </summary>
        /// <remarks>Provided as a LINQ helper.</remarks>
        public static bool UsesActive_SplitterComponent(EntityRef entr) => entr.HasActive_SplitterComponent;

        #endregion
        #region Basic-LINQ MonitorComponent

        /// <summary>
        /// True if the entity's monitorId does not point to the null MonitorComponent
        /// </summary>
        /// <remarks>Provided as a LINQ helper.</remarks>
        public static bool Uses_MonitorComponent(EntityRef entr) => entr.Has_MonitorComponent;
        /// <summary>
        /// True if the entity's MonitorComponent in the factory matches the entity.
        /// </summary>
        /// <remarks>Provided as a LINQ helper.</remarks>
        public static bool UsesActive_MonitorComponent(EntityRef entr) => entr.HasActive_MonitorComponent;

        #endregion
        #region Basic-LINQ SpraycoaterComponent

        /// <summary>
        /// True if the entity's spraycoaterId does not point to the null SpraycoaterComponent
        /// </summary>
        /// <remarks>Provided as a LINQ helper.</remarks>
        public static bool Uses_SpraycoaterComponent(EntityRef entr) => entr.Has_SpraycoaterComponent;
        /// <summary>
        /// True if the entity's SpraycoaterComponent in the factory matches the entity.
        /// </summary>
        /// <remarks>Provided as a LINQ helper.</remarks>
        public static bool UsesActive_SpraycoaterComponent(EntityRef entr) => entr.HasActive_SpraycoaterComponent;

        #endregion
        #region Basic-LINQ PilerComponent

        /// <summary>
        /// True if the entity's pilerId does not point to the null PilerComponent
        /// </summary>
        /// <remarks>Provided as a LINQ helper.</remarks>
        public static bool Uses_PilerComponent(EntityRef entr) => entr.Has_PilerComponent;
        /// <summary>
        /// True if the entity's PilerComponent in the factory matches the entity.
        /// </summary>
        /// <remarks>Provided as a LINQ helper.</remarks>
        public static bool UsesActive_PilerComponent(EntityRef entr) => entr.HasActive_PilerComponent;

        #endregion
        #region Basic-LINQ SpeakerComponent

        /// <summary>
        /// True if the entity's speakerId does not point to the null SpeakerComponent
        /// </summary>
        /// <remarks>Provided as a LINQ helper.</remarks>
        public static bool Uses_SpeakerComponent(EntityRef entr) => entr.Has_SpeakerComponent;
        /// <summary>
        /// True if the entity's SpeakerComponent in the factory matches the entity.
        /// </summary>
        /// <remarks>Provided as a LINQ helper.</remarks>
        public static bool UsesActive_SpeakerComponent(EntityRef entr) => entr.HasActive_SpeakerComponent;

        #endregion
        #region Basic-LINQ StorageComponent

        /// <summary>
        /// True if the entity's storageId does not point to the null StorageComponent
        /// </summary>
        /// <remarks>Provided as a LINQ helper.</remarks>
        public static bool Uses_StorageComponent(EntityRef entr) => entr.Has_StorageComponent;
        /// <summary>
        /// True if the entity's StorageComponent in the factory matches the entity.
        /// </summary>
        /// <remarks>Provided as a LINQ helper.</remarks>
        public static bool UsesActive_StorageComponent(EntityRef entr) => entr.HasActive_StorageComponent;

        #endregion
        #region Basic-LINQ TankComponent

        /// <summary>
        /// True if the entity's tankId does not point to the null TankComponent
        /// </summary>
        /// <remarks>Provided as a LINQ helper.</remarks>
        public static bool Uses_TankComponent(EntityRef entr) => entr.Has_TankComponent;
        /// <summary>
        /// True if the entity's TankComponent in the factory matches the entity.
        /// </summary>
        /// <remarks>Provided as a LINQ helper.</remarks>
        public static bool UsesActive_TankComponent(EntityRef entr) => entr.HasActive_TankComponent;

        #endregion
        #region Basic-LINQ MinerComponent

        /// <summary>
        /// True if the entity's minerId does not point to the null MinerComponent
        /// </summary>
        /// <remarks>Provided as a LINQ helper.</remarks>
        public static bool Uses_MinerComponent(EntityRef entr) => entr.Has_MinerComponent;
        /// <summary>
        /// True if the entity's MinerComponent in the factory matches the entity.
        /// </summary>
        /// <remarks>Provided as a LINQ helper.</remarks>
        public static bool UsesActive_MinerComponent(EntityRef entr) => entr.HasActive_MinerComponent;

        #endregion
        #region Basic-LINQ InserterComponent

        /// <summary>
        /// True if the entity's inserterId does not point to the null InserterComponent
        /// </summary>
        /// <remarks>Provided as a LINQ helper.</remarks>
        public static bool Uses_InserterComponent(EntityRef entr) => entr.Has_InserterComponent;
        /// <summary>
        /// True if the entity's InserterComponent in the factory matches the entity.
        /// </summary>
        /// <remarks>Provided as a LINQ helper.</remarks>
        public static bool UsesActive_InserterComponent(EntityRef entr) => entr.HasActive_InserterComponent;

        #endregion
        #region Basic-LINQ AssemblerComponent

        /// <summary>
        /// True if the entity's assemblerId does not point to the null AssemblerComponent
        /// </summary>
        /// <remarks>Provided as a LINQ helper.</remarks>
        public static bool Uses_AssemblerComponent(EntityRef entr) => entr.Has_AssemblerComponent;
        /// <summary>
        /// True if the entity's AssemblerComponent in the factory matches the entity.
        /// </summary>
        /// <remarks>Provided as a LINQ helper.</remarks>
        public static bool UsesActive_AssemblerComponent(EntityRef entr) => entr.HasActive_AssemblerComponent;

        #endregion
        #region Basic-LINQ FractionatorComponent

        /// <summary>
        /// True if the entity's fractionatorId does not point to the null FractionatorComponent
        /// </summary>
        /// <remarks>Provided as a LINQ helper.</remarks>
        public static bool Uses_FractionatorComponent(EntityRef entr) => entr.Has_FractionatorComponent;
        /// <summary>
        /// True if the entity's FractionatorComponent in the factory matches the entity.
        /// </summary>
        /// <remarks>Provided as a LINQ helper.</remarks>
        public static bool UsesActive_FractionatorComponent(EntityRef entr) => entr.HasActive_FractionatorComponent;

        #endregion
        #region Basic-LINQ EjectorComponent

        /// <summary>
        /// True if the entity's ejectorId does not point to the null EjectorComponent
        /// </summary>
        /// <remarks>Provided as a LINQ helper.</remarks>
        public static bool Uses_EjectorComponent(EntityRef entr) => entr.Has_EjectorComponent;
        /// <summary>
        /// True if the entity's EjectorComponent in the factory matches the entity.
        /// </summary>
        /// <remarks>Provided as a LINQ helper.</remarks>
        public static bool UsesActive_EjectorComponent(EntityRef entr) => entr.HasActive_EjectorComponent;

        #endregion
        #region Basic-LINQ SiloComponent

        /// <summary>
        /// True if the entity's siloId does not point to the null SiloComponent
        /// </summary>
        /// <remarks>Provided as a LINQ helper.</remarks>
        public static bool Uses_SiloComponent(EntityRef entr) => entr.Has_SiloComponent;
        /// <summary>
        /// True if the entity's SiloComponent in the factory matches the entity.
        /// </summary>
        /// <remarks>Provided as a LINQ helper.</remarks>
        public static bool UsesActive_SiloComponent(EntityRef entr) => entr.HasActive_SiloComponent;

        #endregion
        #region Basic-LINQ LabComponent

        /// <summary>
        /// True if the entity's labId does not point to the null LabComponent
        /// </summary>
        /// <remarks>Provided as a LINQ helper.</remarks>
        public static bool Uses_LabComponent(EntityRef entr) => entr.Has_LabComponent;
        /// <summary>
        /// True if the entity's LabComponent in the factory matches the entity.
        /// </summary>
        /// <remarks>Provided as a LINQ helper.</remarks>
        public static bool UsesActive_LabComponent(EntityRef entr) => entr.HasActive_LabComponent;

        #endregion
        #region Basic-LINQ StationComponent

        /// <summary>
        /// True if the entity's stationId does not point to the null StationComponent
        /// </summary>
        /// <remarks>Provided as a LINQ helper.</remarks>
        public static bool Uses_StationComponent(EntityRef entr) => entr.Has_StationComponent;
        /// <summary>
        /// True if the entity's StationComponent in the factory matches the entity.
        /// </summary>
        /// <remarks>Provided as a LINQ helper.</remarks>
        public static bool UsesActive_StationComponent(EntityRef entr) => entr.HasActive_StationComponent;

        #endregion
        #region Basic-LINQ PowerGeneratorComponent

        /// <summary>
        /// True if the entity's powerGenId does not point to the null PowerGeneratorComponent
        /// </summary>
        /// <remarks>Provided as a LINQ helper.</remarks>
        public static bool Uses_PowerGeneratorComponent(EntityRef entr) => entr.Has_PowerGeneratorComponent;
        /// <summary>
        /// True if the entity's PowerGeneratorComponent in the factory matches the entity.
        /// </summary>
        /// <remarks>Provided as a LINQ helper.</remarks>
        public static bool UsesActive_PowerGeneratorComponent(EntityRef entr) => entr.HasActive_PowerGeneratorComponent;

        #endregion
        #region Basic-LINQ PowerNodeComponent

        /// <summary>
        /// True if the entity's powerNodeId does not point to the null PowerNodeComponent
        /// </summary>
        /// <remarks>Provided as a LINQ helper.</remarks>
        public static bool Uses_PowerNodeComponent(EntityRef entr) => entr.Has_PowerNodeComponent;
        /// <summary>
        /// True if the entity's PowerNodeComponent in the factory matches the entity.
        /// </summary>
        /// <remarks>Provided as a LINQ helper.</remarks>
        public static bool UsesActive_PowerNodeComponent(EntityRef entr) => entr.HasActive_PowerNodeComponent;

        #endregion
        #region Basic-LINQ PowerConsumerComponent

        /// <summary>
        /// True if the entity's powerConId does not point to the null PowerConsumerComponent
        /// </summary>
        /// <remarks>Provided as a LINQ helper.</remarks>
        public static bool Uses_PowerConsumerComponent(EntityRef entr) => entr.Has_PowerConsumerComponent;
        /// <summary>
        /// True if the entity's PowerConsumerComponent in the factory matches the entity.
        /// </summary>
        /// <remarks>Provided as a LINQ helper.</remarks>
        public static bool UsesActive_PowerConsumerComponent(EntityRef entr) => entr.HasActive_PowerConsumerComponent;

        #endregion
        #region Basic-LINQ PowerAccumulatorComponent

        /// <summary>
        /// True if the entity's powerAccId does not point to the null PowerAccumulatorComponent
        /// </summary>
        /// <remarks>Provided as a LINQ helper.</remarks>
        public static bool Uses_PowerAccumulatorComponent(EntityRef entr) => entr.Has_PowerAccumulatorComponent;
        /// <summary>
        /// True if the entity's PowerAccumulatorComponent in the factory matches the entity.
        /// </summary>
        /// <remarks>Provided as a LINQ helper.</remarks>
        public static bool UsesActive_PowerAccumulatorComponent(EntityRef entr) => entr.HasActive_PowerAccumulatorComponent;

        #endregion
        #region Basic-LINQ PowerExchangerComponent

        /// <summary>
        /// True if the entity's powerExcId does not point to the null PowerExchangerComponent
        /// </summary>
        /// <remarks>Provided as a LINQ helper.</remarks>
        public static bool Uses_PowerExchangerComponent(EntityRef entr) => entr.Has_PowerExchangerComponent;
        /// <summary>
        /// True if the entity's PowerExchangerComponent in the factory matches the entity.
        /// </summary>
        /// <remarks>Provided as a LINQ helper.</remarks>
        public static bool UsesActive_PowerExchangerComponent(EntityRef entr) => entr.HasActive_PowerExchangerComponent;

        #endregion
    }
}