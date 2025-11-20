public class StateNamer
{
    public static void UpdateStateNames(State state)
    {
        switch (state.government)
        {
            case GovernmentType.REPUBLIC:
                switch (state.vassalManager.sovereignty)
                {
                    case Sovereignty.COLONY:
                        state.govtName = "Colony";
                        state.leaderTitle = "Administrator";
                        break;
                    case Sovereignty.PUPPET:
                        state.govtName = "Mandate";
                        state.leaderTitle = "Governor";
                        break;
                    case Sovereignty.PROVINCE:
                        state.govtName = "Department";
                        state.leaderTitle = "Governor";
                        break;
                    default:
                        state.govtName = "Free State";
                        state.leaderTitle = "Prime Minister";
                        if (state.vassalManager.vassalIds.Count > 0)
                        {
                            state.govtName = "Republic";
                            state.leaderTitle = "President";
                        }
                        else if (state.vassalManager.vassalIds.Count > 3)
                        {
                            state.govtName = "Commonwealth";
                            state.leaderTitle = "Chancellor";
                        }
                        break;
                }
                break;
            case GovernmentType.MONARCHY:
                switch (state.vassalManager.sovereignty)
                {
                    case Sovereignty.COLONY:
                        state.govtName = "Crown Colony";
                        state.leaderTitle = "Viceroy";
                        break;
                    case Sovereignty.PUPPET:
                        state.govtName = "Protectorate";
                        state.leaderTitle = "Regent";
                        break;
                    case Sovereignty.PROVINCE:
                        state.govtName = "Duchy";
                        state.leaderTitle = "Duke";
                        break;
                    default:
                        state.govtName = "Principality";
                        state.leaderTitle = "Prince";
                        if (state.vassalManager.vassalIds.Count > 0)
                        {
                            state.govtName = "Kingdom";
                            state.leaderTitle = "King";
                        }
                        else if (state.vassalManager.vassalIds.Count > 3)
                        {
                            state.govtName = "Empire";
                            state.leaderTitle = "Emperor";
                        }
                        break;
                }
                break;
            case GovernmentType.AUTOCRACY:
                switch (state.vassalManager.sovereignty)
                {
                    case Sovereignty.COLONY:
                        state.govtName = "Territory";
                        state.leaderTitle = "Governor-General";
                        break;
                    case Sovereignty.PUPPET:
                        state.govtName = "Client State";
                        state.leaderTitle = "Administrator";
                        break;
                    case Sovereignty.PROVINCE:
                        state.govtName = "Province";
                        state.leaderTitle = "Governor";
                        break;
                    default:
                        state.govtName = "State";
                        state.leaderTitle = "Despot";
                        if (state.vassalManager.vassalIds.Count > 0)
                        {
                            state.govtName = "Autocracy";
                            state.leaderTitle = "Archon";
                        }
                        else if (state.vassalManager.vassalIds.Count > 3)
                        {
                            state.govtName = "Imperium";
                            state.leaderTitle = "Emperor";
                        }
                        break;
                }
                break;
            default:
                state.govtName = "State";
                break;
        }
        state.name = $"{state.govtName} of {state.baseName}";
    }
}