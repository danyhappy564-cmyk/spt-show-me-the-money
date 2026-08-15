using EFT;
using EFT.InventoryLogic;

namespace SwiftXP.SPT.ShowMeTheMoney.Client.Utilities;

public static class ItemCloneUtility
{
    public static T CloneForPricing<T>(this T item, bool stripChildSlots = false) where T : Item
    {
        T clone = item.CloneItem(TemporaryIdGenerator.Instance);
        if (stripChildSlots && clone is CompoundItem compoundItem)
            compoundItem.Slots = [];

        return clone;
    }

    private sealed class TemporaryIdGenerator : IDatabaseIdGenerator
    {
        public static readonly TemporaryIdGenerator Instance = new();

        public MongoID NextId => MongoID.Generate();

        public void RollBack()
        {
        }
    }
}
