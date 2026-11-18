using System;
using System.Collections.Generic;
using System.Linq;
using Arvito;
using V2 = System.Numerics.Vector2;

internal static class Program
{
    private static int checks;
    private const float Tick = 1f / 60f;

    private static void Check(bool condition, string description)
    {
        if (!condition) throw new InvalidOperationException(description);
        checks++;
    }

    private static bool Close(float a, float b) => Math.Abs(a-b) < .002f;

    private static void Fit(Inventory inventory, Loadout rig, ItemId id)
    {
        Check(inventory.Install(rig,Parts.Get(id).Socket,id,out string reason),reason);
    }

    private static void ChangeChip(Unit unit, ItemId id)
    {
        var bag = new Inventory();
        bag.Add(id);
        Fit(bag,unit.Rig,id);
    }

    private static void InventoryAndPower()
    {
        var field = new Field();
        var rig = field.Player.Rig;
        Check(rig.Online && rig.Demand == 8 && rig.Capacity == 9,"Default rig is powered");
        Check(field.Interact() && !field.Interact(),"A cache can be stripped just once");
        var bag = field.Inventory;
        Check(bag.Count(ItemId.Relay) == 1 && bag.Count(ItemId.Dynamo) == 1,"Cache contains hardware and code");
        bag.Add(ItemId.Rail);
        Check(!bag.Install(rig,Socket.Weapon,ItemId.Rail,out _),"Insufficient power rejects the swap");
        Check(bag.Count(ItemId.Rail) == 1 && rig[Socket.Weapon] == ItemId.Riveter,"Rejected swap is atomic");
        Check(!bag.Install(rig,Socket.When,ItemId.Relay,out _),"Chip cannot cross socket types");
        Check(!bag.Install(rig,Socket.How,ItemId.Fire,out _),"Cannot spend an unowned chip");
        Fit(bag,rig,ItemId.Dynamo); Fit(bag,rig,ItemId.Rail);
        Check(rig.Demand == 13 && rig.Capacity == 13 && bag.Count(ItemId.Riveter) == 1,"Budget and traded parts agree");
        Check(bag.Install(rig,Socket.Why,ItemId.Empty,out _) && !rig.Online && rig.HasDrive,"Empty rule stops fire but preserves drive");
        Check(bag.Count(ItemId.Closest) == 1,"Detached chip enters stock");
        Fit(bag,rig,ItemId.Closest);
        Check(rig.Online && bag.Count(ItemId.Closest) == 0,"Full circuit resumes without duplication");
        field.Player.Integrity = .5f;
        bag.Add(ItemId.Skater);
        Fit(bag,rig,ItemId.Skater);
        Check(Close(field.Player.Health,34),"Body swap keeps percentage of integrity");
        Check(!bag.Install(rig,Socket.Core,ItemId.Cell,out _) && rig[Socket.Core] == ItemId.Dynamo,"Weak core cannot displace a powered core");
        Check(bag.Install(rig,Socket.Hull,ItemId.Empty,out _) && !rig.HasDrive,"Removing hull disables drive");
        for (int i = 1; i < Parts.All.Length; i++)
            Check((int)Parts.All[i].Id == i,"Catalogue ids index the correct part");
        foreach (var enemy in new[]{Rigs.Runner(),Rigs.Sentry(),Rigs.Warden()})
            Check(enemy.Online && enemy.Contents().Length == 9,"Every enemy uses the standard sockets");
    }

    private static void SixSlots()
    {
        var p = new Unit(1,ChassisRole.Player,V2.Zero,Rigs.Surveyor());
        var near = new Unit(2,ChassisRole.Sentry,new V2(0,4),Rigs.Sentry());
        var far = new Unit(3,ChassisRole.Warden,new V2(0,8),Rigs.Warden());
        near.Forward = -V2.UnitY;
        var ally = new Unit(4,ChassisRole.Player,new V2(0,1),Rigs.Surveyor());
        var options = new List<Unit>{p,ally,near,far};
        Unit Select() => Ballistics.Select(p,options,Ballistics.Source(p,0));
        Check(Select() == near,"Default command selects nearest hostile");
        Check(Ballistics.Select(near,options,near.Position) == ally,"Hostile uses identical interpreter with reversed faction");
        far.Integrity = .20f;
        ChangeChip(p,ItemId.Weakest); Check(Select() == far,"Weakest evaluates actual hull points");
        ChangeChip(p,ItemId.Strongest);
        Check(Select() == near,"The rig with higher damage per second wins priority");
        ChangeChip(p,ItemId.Damaged); Check(Select() == far,"Damaged filter drops intact enemies");
        ChangeChip(p,ItemId.ForwardArc); p.Forward = -V2.UnitY;
        Check(Select() == null,"Rear target lies outside the forward cone");
        p.Forward = V2.UnitY; Check(Select() == far,"Cone accepts frontal targets");
        ChangeChip(p,ItemId.NearField);
        Check(Select() == null,"Inner half-range rejects distant targets");
        ChangeChip(p,ItemId.InRange);
        ChangeChip(p,ItemId.Moving); Check(Select() == null,"Moving trigger is idle at rest");
        p.Velocity=V2.UnitX; Check(Select() == far,"Movement enables the trigger");
        ChangeChip(p,ItemId.Stationary); Check(Select() == null,"Stop trigger waits for the hull");
        p.Velocity=V2.Zero; Check(Select() == far,"Stationary trigger activates");
        ChangeChip(p,ItemId.Always);
        ChangeChip(p,ItemId.Relay);
        V2 origin = Ballistics.Source(p,0);
        Check(Close(origin.Length(),1.5f) && V2.Distance(origin,Ballistics.Source(p,1)) > .5f,"Relay is an orbiting firing origin");
        far.Position = origin + V2.Normalize(origin)*10.9f;
        Check(far.Position.Length() > Parts.Get(ItemId.Riveter).Range && Select() == far,"Range starts at relay");
        Check(Ballistics.Select(p,options,origin,(a,b)=>true) == null,"Cover blocks target acquisition");
        far.Position=origin+V2.UnitY*6; far.Velocity=V2.UnitX*3;
        V2 direct=Ballistics.AimAt(p,far,origin);
        ChangeChip(p,ItemId.Lead);
        V2 lead=Ballistics.AimAt(p,far,origin);
        Check(Close(direct.X,0) && lead.X > .1f && Close(lead.Length(),1),"Lead predicts transverse motion");
        Check(Close(p.Rig.At(Socket.Weapon).Damage,16) && Close(p.Rig.At(Socket.Weapon).ShotsPerSecond,1.8f),"Chips preserve physical weapon stats");
        Check(Close(Ballistics.HitDistance(V2.Zero,V2.UnitX,8,new V2(6,0),1),5),"Swept bolt hits first face without tunnelling");
        Check(float.IsPositiveInfinity(Ballistics.HitDistance(V2.Zero,V2.UnitX,1,new V2(-.8f,.8f),1)),
            "A bolt moving away from a clipped circle cannot hit behind its start");
    }

    private static void LootAndDamage()
    {
        var f = new Field();
        var rig = Rigs.Sentry();
        var victim = new Unit(100,ChassisRole.Sentry,f.Player.Position+V2.UnitX*2,rig);
        victim.Integrity=.05f; victim.Reload=30; f.Units.Add(victim);
        f.Interact();
        for (int i=0; i<90 && victim.Alive; i++) f.Step(Tick,V2.Zero);
        Wreck pile = f.Wrecks.Single(w=>!w.Cache);
        Check(pile.Cargo.SequenceEqual(rig.Contents()),"Destroyed enemy drops its actual rig and six chips");
        f.Player.Position=pile.Position;
        f.Player.Integrity=.5f;
        Check(f.Interact() && !f.Interact() && Close(f.Player.Health,72),"Enemy wreck is consumed once and repairs the player");
        Check(f.Inventory.Count(ItemId.Relay)==2 && f.Inventory.Count(ItemId.Stationary)==1,"Hardware and AI are both collected");
        var lose = new Field();
        lose.Inventory.Install(lose.Player.Rig,Socket.How,ItemId.Empty,out _);
        for (int i=0;i<60*90 && lose.Result==Outcome.Active;i++) lose.Step(Tick,V2.Zero);
        Check(lose.Result==Outcome.Destroyed,"An unarmed rig can be destroyed");
        float t=lose.Time; lose.Step(Tick,V2.UnitY);
        Check(lose.Time==t && !lose.Interact(),"Terminal state cannot advance or extract");
    }

    // This route uses only movement, salvage and installation. The live combat
    // simulation supplies every spawn, hit, wreck, new wave and extraction.
    private static void FullSortie()
    {
        var f = new Field();
        Check(f.Interact(),"Start from the relay cache");
        Fit(f.Inventory,f.Player.Rig,ItemId.Dynamo);
        Fit(f.Inventory,f.Player.Rig,ItemId.Lead);
        int waves=0;
        f.Signal += (e,p,friendly)=>{ if (e==FieldEvent.Wave) waves++; };
        for (int i=0;i<60*130 && f.Result==Outcome.Active;i++)
        {
            V2 goal;
            if (f.CanExtract) goal=Field.Beacon;
            else if (f.Player.Rig[Socket.Weapon]!=ItemId.Rail && f.Time<9) goal=new V2(-8,6);
            else
            {
                float angle=f.Time*.37f;
                goal=new V2((float)Math.Cos(angle),(float)Math.Sin(angle))*8.6f;
            }
            f.Step(Tick,Ballistics.Direction(goal-f.Player.Position));
            if (f.Nearby()!=null || f.CanExtract && f.AtBeacon) f.Interact();
            if (f.Inventory.Count(ItemId.Rail)>0 && f.Player.Rig[Socket.Weapon]!=ItemId.Rail)
                Fit(f.Inventory,f.Player.Rig,ItemId.Rail);
        }
        Check(waves==3 && f.Hostiles==0 && f.Result==Outcome.Extracted,"Survive all three waves and dock at the relay");
        Check(f.Recoveries>2 && f.Player.Health>0,"Successful route includes salvaged opponents");
    }

    private static void Main()
    {
        InventoryAndPower(); SixSlots(); LootAndDamage(); FullSortie();
        Console.WriteLine($"Passed {checks} checks, including a full three-wave sortie.");
    }
}
