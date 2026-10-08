using UnityEngine;
using Steamworks;

public static class LobbyCode
{
    public const int Length = 7;
    private const string Alphabet = "23456789ABCDEFGHJKLMNPQRSTUVWXYZ";
    private const ulong LobbyIdPrefix = 0x01860000UL << 32;

    public static string Encode(CSteamID lobbyId)
    {
        ulong id = lobbyId.m_SteamID;
        if ((id & 0xFFFFFFFF00000000UL) != LobbyIdPrefix) {
            Debug.LogError("Lobby id " + id + " has an unexpected prefix");
        }

        uint value = (uint)(id & 0xFFFFFFFFUL);
        char[] code = new char[Length];
        for (int i = Length - 1; i >= 0; i--) {
            code[i] = Alphabet[(int)(value % (uint)Alphabet.Length)];
            value /= (uint)Alphabet.Length;
        }
        return new string(code);
    }

    public static bool TryDecode(string code, out CSteamID lobbyId)
    {
        lobbyId = CSteamID.Nil;
        if (code == null) {
            return false;
        }

        code = code.Trim().ToUpperInvariant();
        if (code.Length != Length) {
            return false;
        }

        ulong value = 0;
        foreach (char c in code) {
            int digit = Alphabet.IndexOf(c);
            if (digit < 0) {
                return false;
            }
            value = value * (ulong)Alphabet.Length + (ulong)digit;
        }
        if (value > uint.MaxValue) {
            return false;
        }

        lobbyId = new CSteamID(LobbyIdPrefix | value);
        return true;
    }
}
