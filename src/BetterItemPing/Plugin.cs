using BepInEx;
using RoR2;
using RoR2.UI;
using System.Linq;
using System.Security;
using System.Security.Permissions;
using UnityEngine;

#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618

namespace BetterItemPing;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
public sealed class Plugin : BaseUnityPlugin
{
    public const string PluginGuid = "com.cirillom.betteritemping";
    public const string PluginName = "Better Item Ping";
    public const string PluginVersion = "1.0.1";

    private void Awake()
    {
        On.RoR2.UI.PingIndicator.RebuildPing += PingIndicatorRebuildPing;
        Logger.LogInfo("Better Item Ping loaded!");
    }

    private static void PingIndicatorRebuildPing(
        On.RoR2.UI.PingIndicator.orig_RebuildPing orig,
        PingIndicator self)
    {
        orig(self);

        if (!self.pingTarget || !TryGetPickupIndex(self.pingTarget, out var pickupIndex))
            return;

        var pickupDef = PickupCatalog.GetPickupDef(pickupIndex);

        if (pickupDef == null ||
            (pickupDef.itemIndex == ItemIndex.None && pickupDef.equipmentIndex == EquipmentIndex.None))
            return;

        var owners = NetworkUser.readOnlyInstancesList
            .Select(user => new
            {
                User = user,
                Count = GetCount(user.master?.inventory, pickupDef)
            })
            .Where(entry => entry.Count > 0)
            .Select(entry =>
                $"{Util.EscapeRichTextForTextMeshPro(entry.User.userName)} ×{entry.Count}")
            .ToArray();

        if (owners.Length > 0)
            Chat.AddMessage($"<style=cSub>Owned by: {string.Join(", ", owners)}</style>");
    }

    private static bool TryGetPickupIndex(GameObject target, out PickupIndex pickupIndex)
    {
        var pickup = target.GetComponentInParent<GenericPickupController>() ??
                     target.GetComponentInChildren<GenericPickupController>();

        if (pickup)
        {
            pickupIndex = pickup.pickup.pickupIndex;
            return pickupIndex.isValid;
        }

        var display = target.GetComponentInChildren<PickupDisplay>();
        pickupIndex = display ? display.GetPickupIndex() : PickupIndex.none;
        return pickupIndex.isValid;
    }

    private static int GetCount(Inventory? inventory, PickupDef pickupDef)
    {
        if (inventory == null)
            return 0;

        if (pickupDef.itemIndex != ItemIndex.None)
            return inventory.GetItemCountEffective(pickupDef.itemIndex);

        return pickupDef.equipmentIndex != EquipmentIndex.None && inventory.HasEquipment(pickupDef.equipmentIndex)
            ? 1
            : 0;
    }
}
