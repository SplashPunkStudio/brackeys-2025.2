using UnityEngine;

public static class InstanceInfo
{

    private static bool m_player1Party1 = true;
    public static bool Player1Party1 => m_player1Party1;

    public static void SetPlayer1Party1(bool value)
    {
        m_player1Party1 = value;
    }

}
