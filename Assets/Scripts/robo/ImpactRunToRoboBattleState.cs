public static class ImpactRunToRoboBattleState
{
    public static bool HasData { get; private set; }

    public static int CurrentHP { get; private set; }
    public static int MaxHP { get; private set; }

    public static int CurrentEnergy { get; private set; }
    public static int MaxEnergy { get; private set; }

    public static void Save(int currentHP, int maxHP, int currentEnergy, int maxEnergy)
    {
        CurrentHP = currentHP;
        MaxHP = maxHP;
        CurrentEnergy = currentEnergy;
        MaxEnergy = maxEnergy;
        HasData = true;
    }

    public static void Clear()
    {
        HasData = false;
        CurrentHP = 0;
        MaxHP = 0;
        CurrentEnergy = 0;
        MaxEnergy = 0;
    }
}