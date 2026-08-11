#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;

namespace OneMoreKnight.EditorTools
{
    /// <summary>
    /// The sprite overhaul tool (#99): a parametric medieval pixel-art library that
    /// redraws every creature in a shared style — outline, two or three fills, one
    /// accent glow — through ~16 genuinely distinct archetypes, so the roster stops
    /// reading as the same knight and the same golem over and over.
    ///
    /// Rules it enforces:
    /// - Every replaced PNG is copied to Assets/Art/Legacy/ FIRST (the revert path).
    /// - Overwrites happen IN PLACE at the original canvas size: same GUID, same
    ///   sprite bounds, so references and collider fit never move.
    /// - All coordinates are normalized (0..1 of the canvas), so one drawer serves a
    ///   22 px fodder enemy and a 116 px boss alike at PPU 32.
    ///
    /// Rerunnable via the menu item; tweak a palette or drawer and forge again.
    /// </summary>
    public static class SpriteForge
    {
        // ---------- canvas ----------
        private static Color32[] px;
        private static int W, H;

        private static void Begin(int w, int h) { W = w; H = h; px = new Color32[w * h]; }

        private static void R(float x0, float y0, float x1, float y1, Color c)
        {
            int ax = Mathf.RoundToInt(x0 * (W - 1)), ay = Mathf.RoundToInt(y0 * (H - 1));
            int bx = Mathf.RoundToInt(x1 * (W - 1)), by = Mathf.RoundToInt(y1 * (H - 1));
            for (int y = Mathf.Min(ay, by); y <= Mathf.Max(ay, by); y++)
                for (int x = Mathf.Min(ax, bx); x <= Mathf.Max(ax, bx); x++)
                    if (x >= 0 && x < W && y >= 0 && y < H) px[y * W + x] = c;
        }

        private static void Disc(float cx, float cy, float radius, Color c)
        {
            float r = radius * (W - 1);
            float px0 = cx * (W - 1), py0 = cy * (H - 1);
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                    if ((x - px0) * (x - px0) + (y - py0) * (y - py0) <= r * r)
                        px[y * W + x] = c;
        }

        // ---------- palettes ----------
        private struct Pal
        {
            public Color O, D, M, L, A; // outline, dark, mid, light, accent
            public Pal(Color o, Color d, Color m, Color l, Color a) { O = o; D = d; M = m; L = l; A = a; }
        }

        private static readonly Color Ink = new Color(0.12f, 0.1f, 0.15f);

        private static Pal Steel   => new Pal(Ink, new Color(0.34f, 0.37f, 0.46f), new Color(0.55f, 0.58f, 0.66f), new Color(0.74f, 0.77f, 0.84f), new Color(0.95f, 0.78f, 0.25f));
        private static Pal Crimson => new Pal(Ink, new Color(0.42f, 0.1f, 0.14f), new Color(0.68f, 0.16f, 0.2f), new Color(0.88f, 0.34f, 0.32f), new Color(0.95f, 0.78f, 0.25f));
        private static Pal Voidp   => new Pal(Ink, new Color(0.2f, 0.09f, 0.3f), new Color(0.38f, 0.18f, 0.54f), new Color(0.56f, 0.34f, 0.74f), new Color(0.85f, 0.6f, 1f));
        private static Pal Bone    => new Pal(Ink, new Color(0.55f, 0.52f, 0.44f), new Color(0.78f, 0.75f, 0.65f), new Color(0.92f, 0.9f, 0.82f), new Color(0.95f, 0.78f, 0.25f));
        private static Pal Verdant => new Pal(Ink, new Color(0.14f, 0.3f, 0.16f), new Color(0.26f, 0.5f, 0.26f), new Color(0.45f, 0.7f, 0.4f), new Color(0.75f, 1f, 0.45f));
        private static Pal Teal    => new Pal(Ink, new Color(0.1f, 0.32f, 0.33f), new Color(0.18f, 0.52f, 0.52f), new Color(0.4f, 0.72f, 0.7f), new Color(0.6f, 1f, 0.95f));
        private static Pal Amber   => new Pal(Ink, new Color(0.5f, 0.3f, 0.1f), new Color(0.75f, 0.5f, 0.16f), new Color(0.92f, 0.68f, 0.3f), new Color(1f, 0.9f, 0.5f));
        private static Pal Ash     => new Pal(Ink, new Color(0.2f, 0.24f, 0.2f), new Color(0.34f, 0.4f, 0.34f), new Color(0.5f, 0.56f, 0.48f), new Color(0.55f, 0.75f, 0.5f));
        private static Pal Pale    => new Pal(Ink, new Color(0.55f, 0.58f, 0.68f), new Color(0.74f, 0.77f, 0.85f), new Color(0.9f, 0.92f, 0.97f), new Color(0.75f, 0.85f, 1f));
        private static Pal Indigo  => new Pal(Ink, new Color(0.16f, 0.16f, 0.36f), new Color(0.27f, 0.27f, 0.55f), new Color(0.42f, 0.44f, 0.75f), new Color(0.6f, 0.65f, 1f));
        private static Pal Wood    => new Pal(Ink, new Color(0.28f, 0.18f, 0.1f), new Color(0.46f, 0.31f, 0.17f), new Color(0.62f, 0.45f, 0.26f), new Color(0.95f, 0.78f, 0.25f));
        private static Pal Black   => new Pal(Ink, new Color(0.12f, 0.11f, 0.14f), new Color(0.2f, 0.18f, 0.23f), new Color(0.32f, 0.3f, 0.36f), new Color(0.7f, 0.15f, 0.2f));
        private static Pal Storm   => new Pal(Ink, new Color(0.15f, 0.22f, 0.4f), new Color(0.24f, 0.36f, 0.6f), new Color(0.4f, 0.55f, 0.8f), new Color(0.7f, 0.9f, 1f));
        private static Pal Blood   => new Pal(Ink, new Color(0.3f, 0.07f, 0.1f), new Color(0.5f, 0.1f, 0.14f), new Color(0.72f, 0.2f, 0.2f), new Color(1f, 0.45f, 0.35f));
        private static Pal HeroPal => new Pal(Ink, new Color(0.2f, 0.3f, 0.5f), new Color(0.35f, 0.5f, 0.75f), new Color(0.55f, 0.7f, 0.9f), new Color(0.95f, 0.78f, 0.25f));

        // ---------- archetypes ----------
        private enum Weapon { None, Sword, Pike, Axe, Torch, Banner }

        private static void Soldier(Pal p, Weapon weapon, bool heavy = false, bool plume = false)
        {
            R(0.36f, 0.02f, 0.44f, 0.2f, p.O); R(0.56f, 0.02f, 0.64f, 0.2f, p.O);      // legs
            R(0.3f, 0.18f, 0.7f, 0.6f, p.O); R(0.33f, 0.2f, 0.67f, 0.58f, p.M);        // torso
            R(0.55f, 0.2f, 0.67f, 0.58f, p.D);
            float pw = heavy ? 0.16f : 0.1f;                                            // pauldrons
            R(0.3f - pw, 0.48f, 0.7f + pw, 0.6f, p.O); R(0.32f - pw, 0.5f, 0.68f + pw, 0.58f, p.L);
            R(0.42f, 0.32f, 0.58f, 0.44f, p.A);                                          // chest sigil
            R(0.36f, 0.6f, 0.64f, 0.88f, p.O); R(0.39f, 0.62f, 0.61f, 0.85f, p.M);      // helm
            R(0.41f, 0.7f, 0.59f, 0.74f, Ink);                                           // visor
            if (plume) { R(0.46f, 0.86f, 0.54f, 0.99f, p.O); R(0.48f, 0.88f, 0.52f, 0.97f, p.A); }
            switch (weapon)
            {
                case Weapon.Sword: R(0.1f, 0.15f, 0.18f, 0.75f, p.O); R(0.12f, 0.2f, 0.16f, 0.7f, p.L);
                    R(0.06f, 0.28f, 0.22f, 0.33f, p.O); break;
                case Weapon.Pike: R(0.11f, 0.05f, 0.16f, 0.9f, p.O); R(0.12f, 0.08f, 0.15f, 0.82f, p.D);
                    R(0.08f, 0.82f, 0.19f, 0.95f, p.L); break;
                case Weapon.Axe: R(0.1f, 0.1f, 0.16f, 0.7f, p.O); R(0.04f, 0.55f, 0.24f, 0.72f, p.O);
                    R(0.06f, 0.58f, 0.22f, 0.69f, p.L); break;
                case Weapon.Torch: R(0.11f, 0.2f, 0.16f, 0.6f, Wood.M);
                    Disc(0.135f, 0.68f, 0.07f, p.A); break;
                case Weapon.Banner: R(0.1f, 0.1f, 0.14f, 0.95f, p.O);
                    R(0.14f, 0.72f, 0.34f, 0.93f, p.O); R(0.15f, 0.74f, 0.32f, 0.91f, p.A); break;
            }
        }

        private static void Archer(Pal p)
        {
            R(0.4f, 0.02f, 0.47f, 0.2f, p.O); R(0.55f, 0.02f, 0.62f, 0.2f, p.O);
            R(0.34f, 0.18f, 0.68f, 0.58f, p.O); R(0.37f, 0.2f, 0.65f, 0.55f, p.M);
            R(0.58f, 0.24f, 0.72f, 0.62f, p.D);                                          // quiver
            R(0.6f, 0.55f, 0.64f, 0.68f, p.L); R(0.66f, 0.55f, 0.7f, 0.66f, p.L);        // arrows
            R(0.38f, 0.58f, 0.62f, 0.82f, p.O); R(0.41f, 0.6f, 0.59f, 0.79f, p.M);       // hood
            R(0.44f, 0.68f, 0.56f, 0.71f, Ink);
            R(0.08f, 0.15f, 0.13f, 0.8f, p.O);                                           // bow stave
            R(0.13f, 0.18f, 0.15f, 0.77f, p.A);                                          // string
        }

        private enum Sigil { None, Ring, Spiral, Bell, Crook, Book }

        private static void Monk(Pal p, Sigil sigil)
        {
            R(0.24f, 0.03f, 0.76f, 0.2f, p.O); R(0.27f, 0.05f, 0.73f, 0.2f, p.D);        // hem
            R(0.3f, 0.18f, 0.7f, 0.62f, p.O); R(0.33f, 0.2f, 0.67f, 0.6f, p.M);          // robe
            R(0.56f, 0.2f, 0.67f, 0.6f, p.D);
            R(0.36f, 0.6f, 0.64f, 0.9f, p.O); R(0.39f, 0.62f, 0.61f, 0.87f, p.M);        // hood
            R(0.42f, 0.72f, 0.47f, 0.76f, p.A); R(0.53f, 0.72f, 0.58f, 0.76f, p.A);      // eyes
            switch (sigil)
            {
                case Sigil.Ring: R(0.42f, 0.3f, 0.58f, 0.46f, p.A); R(0.46f, 0.34f, 0.54f, 0.42f, p.M); break;
                case Sigil.Spiral: R(0.42f, 0.3f, 0.58f, 0.34f, p.A); R(0.54f, 0.34f, 0.58f, 0.46f, p.A);
                    R(0.42f, 0.42f, 0.5f, 0.46f, p.A); R(0.42f, 0.34f, 0.46f, 0.4f, p.A); break;
                case Sigil.Bell: R(0.43f, 0.3f, 0.57f, 0.44f, p.A); R(0.47f, 0.26f, 0.53f, 0.31f, p.A); break;
                case Sigil.Crook: R(0.13f, 0.1f, 0.18f, 0.85f, Wood.M); R(0.13f, 0.8f, 0.28f, 0.86f, Wood.M); break;
                case Sigil.Book: R(0.4f, 0.3f, 0.6f, 0.46f, p.D); R(0.42f, 0.32f, 0.58f, 0.44f, p.L);
                    R(0.49f, 0.32f, 0.51f, 0.44f, p.O); break;
            }
        }

        private static void Witch(Pal p, bool orb = false)
        {
            R(0.26f, 0.03f, 0.74f, 0.16f, p.O); R(0.29f, 0.05f, 0.71f, 0.16f, p.D);      // skirt hem
            R(0.32f, 0.14f, 0.68f, 0.55f, p.O); R(0.35f, 0.16f, 0.65f, 0.53f, p.M);      // dress
            R(0.4f, 0.53f, 0.6f, 0.68f, p.O); R(0.42f, 0.55f, 0.58f, 0.66f, p.L);        // face
            R(0.44f, 0.59f, 0.47f, 0.62f, p.A); R(0.53f, 0.59f, 0.56f, 0.62f, p.A);      // eyes
            R(0.26f, 0.66f, 0.74f, 0.72f, p.O); R(0.29f, 0.67f, 0.71f, 0.71f, p.D);      // brim
            R(0.38f, 0.72f, 0.62f, 0.8f, p.O); R(0.41f, 0.72f, 0.59f, 0.78f, p.D);       // hat base
            R(0.44f, 0.8f, 0.56f, 0.88f, p.O); R(0.46f, 0.8f, 0.54f, 0.86f, p.D);
            R(0.48f, 0.88f, 0.53f, 0.97f, p.O);                                          // hat tip
            if (orb) { R(0.09f, 0.15f, 0.14f, 0.7f, Wood.M); Disc(0.115f, 0.76f, 0.06f, p.A); }
        }

        private static void Ghost(Pal p, bool wings = false, bool crown = false)
        {
            R(0.3f, 0.2f, 0.7f, 0.75f, p.O); R(0.33f, 0.22f, 0.67f, 0.73f, p.M);         // body
            R(0.56f, 0.22f, 0.67f, 0.73f, p.D);
            for (int i = 0; i < 4; i++)                                                   // wavy fringe
            {
                float x0 = 0.31f + i * 0.1f;
                R(x0, 0.08f + (i % 2) * 0.06f, x0 + 0.08f, 0.22f, p.M);
            }
            R(0.4f, 0.55f, 0.46f, 0.62f, p.A); R(0.54f, 0.55f, 0.6f, 0.62f, p.A);        // eyes
            if (wings)
            {
                R(0.06f, 0.42f, 0.3f, 0.52f, p.O); R(0.08f, 0.44f, 0.3f, 0.5f, p.L);
                R(0.7f, 0.42f, 0.94f, 0.52f, p.O); R(0.7f, 0.44f, 0.92f, 0.5f, p.L);
                R(0.1f, 0.52f, 0.26f, 0.6f, p.L); R(0.74f, 0.52f, 0.9f, 0.6f, p.L);
            }
            if (crown)
            {
                R(0.38f, 0.75f, 0.62f, 0.82f, p.O); R(0.4f, 0.76f, 0.6f, 0.8f, p.A);
                R(0.4f, 0.82f, 0.44f, 0.9f, p.A); R(0.48f, 0.82f, 0.52f, 0.92f, p.A); R(0.56f, 0.82f, 0.6f, 0.9f, p.A);
            }
        }

        private static void Skeleton(Pal p, bool shovel = false, bool crowned = false, bool staff = false)
        {
            R(0.38f, 0.02f, 0.45f, 0.18f, p.L); R(0.55f, 0.02f, 0.62f, 0.18f, p.L);      // leg bones
            R(0.36f, 0.16f, 0.64f, 0.24f, p.O); R(0.38f, 0.18f, 0.62f, 0.22f, p.L);      // pelvis
            for (int i = 0; i < 3; i++)                                                   // ribs
            {
                float y = 0.28f + i * 0.09f;
                R(0.34f, y, 0.66f, y + 0.05f, p.O); R(0.36f, y + 0.01f, 0.64f, y + 0.04f, p.L);
            }
            R(0.47f, 0.24f, 0.53f, 0.56f, p.L);                                           // spine
            R(0.36f, 0.56f, 0.64f, 0.88f, p.O); R(0.39f, 0.58f, 0.61f, 0.85f, p.L);       // skull
            R(0.42f, 0.7f, 0.47f, 0.76f, Ink); R(0.53f, 0.7f, 0.58f, 0.76f, Ink);         // sockets
            R(0.44f, 0.6f, 0.56f, 0.63f, Ink);                                            // jaw line
            if (shovel) { R(0.1f, 0.12f, 0.15f, 0.7f, Wood.M); R(0.06f, 0.05f, 0.19f, 0.16f, p.D); }
            if (crowned)
            {
                R(0.38f, 0.86f, 0.62f, 0.92f, p.A);
                R(0.4f, 0.92f, 0.44f, 0.98f, p.A); R(0.48f, 0.92f, 0.52f, 0.99f, p.A); R(0.56f, 0.92f, 0.6f, 0.98f, p.A);
            }
            if (staff) { R(0.85f, 0.1f, 0.9f, 0.8f, Wood.D); Disc(0.875f, 0.84f, 0.06f, p.A); }
        }

        private static void Gargoyle(Pal p)
        {
            R(0.2f, 0.12f, 0.8f, 0.5f, p.O); R(0.23f, 0.14f, 0.77f, 0.48f, p.M);         // crouched mass
            R(0.6f, 0.14f, 0.77f, 0.48f, p.D);
            R(0.05f, 0.3f, 0.24f, 0.85f, p.O); R(0.08f, 0.34f, 0.22f, 0.8f, p.D);        // folded wings
            R(0.76f, 0.3f, 0.95f, 0.85f, p.O); R(0.78f, 0.34f, 0.92f, 0.8f, p.D);
            R(0.34f, 0.46f, 0.66f, 0.78f, p.O); R(0.37f, 0.48f, 0.63f, 0.75f, p.M);      // head
            R(0.4f, 0.6f, 0.46f, 0.66f, p.A); R(0.54f, 0.6f, 0.6f, 0.66f, p.A);          // eyes
            R(0.42f, 0.48f, 0.58f, 0.54f, p.D);                                           // snout
            R(0.36f, 0.76f, 0.42f, 0.9f, p.O); R(0.58f, 0.76f, 0.64f, 0.9f, p.O);        // horns
            R(0.26f, 0.02f, 0.38f, 0.14f, p.O); R(0.62f, 0.02f, 0.74f, 0.14f, p.O);      // claws
        }

        private static void Wyvern(Pal p)
        {
            R(0.02f, 0.5f, 0.4f, 0.6f, p.O); R(0.04f, 0.52f, 0.4f, 0.58f, p.M);          // wings
            R(0.6f, 0.5f, 0.98f, 0.6f, p.O); R(0.6f, 0.52f, 0.96f, 0.58f, p.M);
            R(0.08f, 0.38f, 0.3f, 0.52f, p.D); R(0.7f, 0.38f, 0.92f, 0.52f, p.D);        // wing membrane
            R(0.42f, 0.15f, 0.58f, 0.75f, p.O); R(0.45f, 0.18f, 0.55f, 0.72f, p.M);      // body
            R(0.47f, 0.02f, 0.53f, 0.18f, p.O);                                          // tail
            R(0.4f, 0.72f, 0.6f, 0.92f, p.O); R(0.43f, 0.74f, 0.57f, 0.89f, p.M);        // head
            R(0.45f, 0.8f, 0.49f, 0.84f, p.A); R(0.51f, 0.8f, 0.55f, 0.84f, p.A);
            R(0.46f, 0.72f, 0.54f, 0.76f, p.D);                                           // snout
        }

        private static void Beast(Pal p, bool bigFangs = false)
        {
            R(0.2f, 0.02f, 0.28f, 0.18f, p.O); R(0.36f, 0.02f, 0.44f, 0.16f, p.O);       // legs
            R(0.56f, 0.02f, 0.64f, 0.16f, p.O); R(0.72f, 0.02f, 0.8f, 0.18f, p.O);
            R(0.14f, 0.14f, 0.86f, 0.5f, p.O); R(0.17f, 0.16f, 0.83f, 0.48f, p.M);       // body
            R(0.62f, 0.16f, 0.83f, 0.48f, p.D);
            R(0.3f, 0.44f, 0.7f, 0.8f, p.O); R(0.33f, 0.46f, 0.67f, 0.77f, p.M);         // head
            R(0.3f, 0.76f, 0.4f, 0.92f, p.O); R(0.6f, 0.76f, 0.7f, 0.92f, p.O);          // ears
            R(0.38f, 0.62f, 0.44f, 0.68f, p.A); R(0.56f, 0.62f, 0.62f, 0.68f, p.A);      // eyes
            float fl = bigFangs ? 0.12f : 0.07f;
            R(0.4f, 0.46f - fl, 0.44f, 0.48f, Color.white); R(0.56f, 0.46f - fl, 0.6f, 0.48f, Color.white);
        }

        private static void Serpent(Pal p, bool maw = false)
        {
            R(0.15f, 0.08f, 0.7f, 0.24f, p.O); R(0.18f, 0.1f, 0.68f, 0.22f, p.M);        // coil 1
            R(0.3f, 0.28f, 0.85f, 0.44f, p.O); R(0.32f, 0.3f, 0.82f, 0.42f, p.M);        // coil 2
            R(0.15f, 0.48f, 0.7f, 0.64f, p.O); R(0.18f, 0.5f, 0.68f, 0.62f, p.D);        // coil 3
            R(0.55f, 0.6f, 0.85f, 0.92f, p.O); R(0.58f, 0.62f, 0.82f, 0.89f, p.M);       // head
            R(0.62f, 0.76f, 0.67f, 0.82f, p.A); R(0.73f, 0.76f, 0.78f, 0.82f, p.A);
            if (maw)
            {
                R(0.6f, 0.62f, 0.8f, 0.72f, Ink);
                R(0.62f, 0.68f, 0.65f, 0.74f, Color.white); R(0.75f, 0.68f, 0.78f, 0.74f, Color.white);
            }
        }

        private static void Crow(Pal p)
        {
            R(0.05f, 0.5f, 0.4f, 0.62f, p.O); R(0.07f, 0.52f, 0.38f, 0.6f, p.D);         // wings
            R(0.6f, 0.5f, 0.95f, 0.62f, p.O); R(0.62f, 0.52f, 0.93f, 0.6f, p.D);
            R(0.36f, 0.25f, 0.64f, 0.6f, p.O); R(0.39f, 0.28f, 0.61f, 0.58f, p.M);       // body
            R(0.42f, 0.55f, 0.58f, 0.8f, p.O); R(0.45f, 0.57f, 0.55f, 0.77f, p.M);       // head
            R(0.46f, 0.66f, 0.49f, 0.7f, p.A); R(0.51f, 0.66f, 0.54f, 0.7f, p.A);
            R(0.47f, 0.5f, 0.53f, 0.58f, Amber.M);                                        // beak
            R(0.44f, 0.08f, 0.56f, 0.28f, p.D);                                           // tail
        }

        private static void Siege(Pal p, bool gate = false)
        {
            if (gate)
            {
                R(0.1f, 0.05f, 0.9f, 0.85f, p.O); R(0.13f, 0.08f, 0.87f, 0.82f, p.M);
                for (int i = 0; i < 4; i++) { float x = 0.2f + i * 0.18f; R(x, 0.08f, x + 0.05f, 0.82f, p.D); }
                for (int i = 0; i < 3; i++) { float y = 0.2f + i * 0.22f; R(0.13f, y, 0.87f, y + 0.05f, p.D); }
                R(0.3f, 0.85f, 0.7f, 0.95f, p.O); R(0.33f, 0.86f, 0.67f, 0.93f, p.L);
                return;
            }
            Disc(0.25f, 0.14f, 0.11f, p.O); Disc(0.25f, 0.14f, 0.08f, p.D);              // wheels
            Disc(0.75f, 0.14f, 0.11f, p.O); Disc(0.75f, 0.14f, 0.08f, p.D);
            R(0.15f, 0.2f, 0.85f, 0.32f, p.O); R(0.18f, 0.22f, 0.82f, 0.3f, p.M);        // frame
            R(0.3f, 0.3f, 0.42f, 0.65f, p.O); R(0.58f, 0.3f, 0.7f, 0.65f, p.O);          // A-frame
            R(0.33f, 0.32f, 0.4f, 0.62f, p.M); R(0.6f, 0.32f, 0.67f, 0.62f, p.M);
            R(0.2f, 0.6f, 0.85f, 0.7f, p.O); R(0.23f, 0.62f, 0.82f, 0.68f, p.L);         // arm
            R(0.78f, 0.68f, 0.9f, 0.82f, p.O); R(0.8f, 0.7f, 0.88f, 0.8f, p.D);          // sling
        }

        private static void Slab(Pal p, bool skull = false)
        {
            R(0.08f, 0.06f, 0.92f, 0.94f, p.O); R(0.12f, 0.1f, 0.88f, 0.9f, p.M);
            R(0.12f, 0.1f, 0.2f, 0.9f, p.L); R(0.8f, 0.1f, 0.88f, 0.9f, p.D);            // edge light
            R(0.24f, 0.16f, 0.76f, 0.22f, p.D); R(0.24f, 0.78f, 0.76f, 0.84f, p.D);      // bands
            if (skull)
            {
                R(0.36f, 0.4f, 0.64f, 0.68f, p.O); R(0.39f, 0.42f, 0.61f, 0.65f, Bone.L);
                R(0.43f, 0.52f, 0.48f, 0.58f, Ink); R(0.52f, 0.52f, 0.57f, 0.58f, Ink);
            }
            else { R(0.45f, 0.3f, 0.55f, 0.7f, p.A); R(0.35f, 0.45f, 0.65f, 0.55f, p.A); }
        }

        private static void DemonLord(Pal p, Weapon weapon = Weapon.None)
        {
            R(0.2f, 0.04f, 0.8f, 0.55f, p.O); R(0.24f, 0.07f, 0.76f, 0.52f, p.D);        // cloaked mass
            R(0.3f, 0.2f, 0.7f, 0.6f, p.O); R(0.33f, 0.22f, 0.67f, 0.58f, p.M);          // torso
            R(0.42f, 0.32f, 0.58f, 0.46f, p.A);                                           // chest glow
            R(0.12f, 0.15f, 0.24f, 0.28f, p.O); R(0.76f, 0.15f, 0.88f, 0.28f, p.O);      // claws
            R(0.36f, 0.58f, 0.64f, 0.84f, p.O); R(0.39f, 0.6f, 0.61f, 0.81f, p.M);       // head
            R(0.41f, 0.68f, 0.47f, 0.74f, p.A); R(0.53f, 0.68f, 0.59f, 0.74f, p.A);
            R(0.3f, 0.78f, 0.38f, 0.98f, p.O); R(0.62f, 0.78f, 0.7f, 0.98f, p.O);        // horns
            R(0.32f, 0.8f, 0.36f, 0.94f, p.D); R(0.64f, 0.8f, 0.68f, 0.94f, p.D);
            if (weapon == Weapon.Axe)
            {
                R(0.06f, 0.2f, 0.12f, 0.85f, p.O); R(0.0f, 0.66f, 0.2f, 0.86f, p.O);
                R(0.02f, 0.69f, 0.17f, 0.83f, Steel.L);
            }
        }

        private static void CrownedLord(Pal p, int crownPoints)
        {
            R(0.22f, 0.05f, 0.78f, 0.5f, p.O); R(0.25f, 0.07f, 0.75f, 0.48f, p.D);       // robe
            R(0.3f, 0.16f, 0.7f, 0.58f, p.O); R(0.33f, 0.18f, 0.67f, 0.55f, p.M);        // torso
            R(0.16f, 0.46f, 0.84f, 0.58f, p.O); R(0.18f, 0.48f, 0.82f, 0.56f, p.L);      // shoulders
            R(0.42f, 0.28f, 0.58f, 0.42f, p.A);                                           // emblem
            R(0.37f, 0.56f, 0.63f, 0.82f, p.O); R(0.4f, 0.58f, 0.6f, 0.79f, p.M);        // head
            R(0.42f, 0.66f, 0.58f, 0.7f, Ink);                                            // visor
            R(0.34f, 0.8f, 0.66f, 0.86f, p.O); R(0.36f, 0.81f, 0.64f, 0.85f, p.A);       // crown band
            for (int i = 0; i < crownPoints; i++)
            {
                float x = 0.37f + i * (0.24f / Mathf.Max(1, crownPoints - 1));
                R(x, 0.85f, x + 0.04f, 0.96f, p.A);
            }
            R(0.88f, 0.1f, 0.93f, 0.7f, p.O); Disc(0.905f, 0.75f, 0.05f, p.A);           // scepter
        }

        private static void WitchQueen(Pal p)
        {
            Witch(p, orb: true);
            R(0.42f, 0.86f, 0.58f, 0.9f, p.A);                                            // hat circlet
            R(0.2f, 0.03f, 0.8f, 0.1f, p.O); R(0.23f, 0.05f, 0.77f, 0.09f, p.A);         // hem trim
        }

        private static void GhostMonarch(Pal p)
        {
            Ghost(p, wings: false, crown: true);
            R(0.24f, 0.3f, 0.32f, 0.6f, p.D); R(0.68f, 0.3f, 0.76f, 0.6f, p.D);          // tattered robe
            R(0.84f, 0.2f, 0.89f, 0.7f, p.O); Disc(0.865f, 0.74f, 0.05f, p.A);           // scepter
        }

        // ---------- the roster ----------
        private static readonly Dictionary<string, System.Action> Map = new Dictionary<string, System.Action>
        {
            // hero + skins
            { "hero", () => Soldier(HeroPal, Weapon.Sword, heavy: true, plume: true) },
            { "RiftKnight", () => Soldier(Voidp, Weapon.Sword, heavy: true, plume: true) },
            // original roster
            { "Descender", () => Soldier(Steel, Weapon.Sword) },
            { "Weaver", () => Witch(Teal) },
            { "Sentinel", () => Soldier(Steel, Weapon.Pike, heavy: true) },
            { "Darter", () => Crow(Black) },
            { "Warlock", () => Monk(Voidp, Sigil.Ring) },
            { "Bulwark", () => Slab(Steel) },
            { "Spinner", () => Monk(Amber, Sigil.Spiral) },
            { "Hexer", () => Witch(Voidp) },
            { "Hunter", () => Beast(Wood) },
            { "Wisp", () => Ghost(Pale) },
            { "Lancer", () => Soldier(Indigo, Weapon.Pike) },
            { "Skirmisher", () => Archer(Wood) },
            { "Marauder", () => Soldier(Crimson, Weapon.Axe) },
            { "Orbiter", () => Monk(Teal, Sigil.Ring) },
            { "Pyromancer", () => Witch(Amber, orb: true) },
            { "Ravager", () => Beast(Black, bigFangs: true) },
            { "Chanter", () => Monk(Amber, Sigil.Bell) },
            { "Hexblade", () => Soldier(Voidp, Weapon.Sword) },
            { "Harbinger", () => Wyvern(Voidp) },
            { "Warden", () => Monk(Verdant, Sigil.Crook) },
            { "Zealot", () => Soldier(Bone, Weapon.Banner) },
            { "Colossus", () => Gargoyle(Ash) },
            { "Aegis", () => Slab(Storm) },
            { "Inquisitor", () => Monk(Crimson, Sigil.Book) },
            { "Seraph", () => Ghost(Bone, wings: true) },
            { "DreadCaptain", () => Soldier(Black, Weapon.Sword, heavy: true, plume: true) },
            { "Reliquary", () => Slab(Voidp, skull: true) },
            { "Grudge", () => Ghost(Voidp) },
            { "Curseweaver", () => Witch(Black) },
            { "Penitent", () => Monk(Pale, Sigil.None) },
            { "Stalker", () => Beast(Ash) },
            { "Gemini", () => Ghost(Verdant) },
            { "Bloodhound", () => Beast(Blood) },
            { "Lamprey", () => Serpent(Pale, maw: true) },
            { "Vulture", () => Crow(Amber) },
            { "Shepherd", () => Monk(Ash, Sigil.Crook) },
            { "Locust", () => Crow(Verdant) },
            { "Direhound", () => Beast(Wood, bigFangs: true) },
            // 1.2.0 batch, de-cloned
            { "Torchbearer", () => Soldier(Amber, Weapon.Torch) },
            { "Pikeman", () => Soldier(Steel, Weapon.Pike) },
            { "Falconer", () => Archer(Amber) },
            { "Runescribe", () => Monk(Teal, Sigil.Book) },
            { "Alchemist", () => Witch(Verdant, orb: true) },
            { "Banneret", () => Soldier(Crimson, Weapon.Banner, heavy: true) },
            { "Ironmonger", () => Slab(Black) },
            { "Bellringer", () => Monk(Wood, Sigil.Bell) },
            { "Gravedigger", () => Skeleton(Ash, shovel: true) },
            { "Martyr", () => Ghost(Bone) },
            { "Dirgesinger", () => Ghost(Indigo, wings: true) },
            { "Trebuchet", () => Siege(Wood) },
            { "Blackguard", () => Soldier(Black, Weapon.Axe) },
            { "Riftling", () => Ghost(Voidp, wings: true) },
            { "Voidcaller", () => Witch(Voidp, orb: true) },
            { "StandardBearer", () => Soldier(Crimson, Weapon.Banner, heavy: true, plume: true) },
            { "Ossuary", () => Slab(Bone, skull: true) },
            { "Riftmaw", () => Serpent(Voidp, maw: true) },
            // bosses
            { "BossGatekeeper", () => Siege(Steel, gate: true) },
            { "BossSerpent", () => Serpent(Verdant, maw: true) },
            { "BossTyrant", () => CrownedLord(Crimson, 3) },
            { "BossMonarch", () => CrownedLord(Amber, 5) },
            { "BossHerald", () => Soldier(Pale, Weapon.Banner, heavy: true, plume: true) },
            { "BossLich", () => Skeleton(Teal, crowned: true, staff: true) },
            { "BossStormcaller", () => WitchQueen(Storm) },
            { "BossBehemoth", () => Gargoyle(Black) },
            { "BossArchon", () => CrownedLord(Bone, 4) },
            { "BossRiftwarden", () => DemonLord(Voidp) },
            { "BossExecutioner", () => DemonLord(Black, Weapon.Axe) },
            { "BossOblivion", () => GhostMonarch(Black) },
            { "BossTribunal", () => Monk(Voidp, Sigil.Book) },
            { "BossVoidmother", () => WitchQueen(Voidp) },
            { "BossWarbringer", () => DemonLord(Crimson, Weapon.Axe) },
            { "BossApocrypha", () => Skeleton(Voidp, crowned: true, staff: true) },
            { "BossEternity", () => GhostMonarch(Amber) },
            { "BossSummoner", () => WitchQueen(Verdant) },
            // BossCastellan and BossBaron keep their 1.2.0 sprites - the keep-tower
            // and crowned knight were already unique; forging them again would only
            // create the clones this issue removes.
            { "BossSquire", () => Soldier(Storm, Weapon.Sword) },
            { "BossPlaguebearer", () => DemonLord(Verdant) },
            { "BossRunesmith", () => WitchQueen(Teal) },
            { "BossWarlord", () => Soldier(Steel, Weapon.Axe, heavy: true, plume: true) },
            { "BossGravewright", () => Skeleton(Ash, shovel: true, crowned: true) },
            { "BossCovenant", () => GhostMonarch(Voidp) },
            { "BossIconoclast", () => Gargoyle(Bone) },
            { "BossRegentBlade", () => Soldier(Blood, Weapon.Sword, heavy: true) },
            { "BossRegent", () => CrownedLord(Blood, 4) },
            { "BossPaleHerald", () => Ghost(Pale, wings: true) },
            { "BossPaleKing", () => GhostMonarch(Pale) },
        };

        // Same archetype+palette twice is allowed ONLY for pairs that shared art on
        // purpose before; everything else must differ. Reviewed by DuplicateReport.
        [MenuItem("One More Knight/Forge Sprites (#99)")]
        public static void Forge()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Art/Legacy"))
                AssetDatabase.CreateFolder("Assets/Art", "Legacy");

            int forged = 0;
            foreach (var entry in Map)
            {
                string path = "Assets/Art/" + entry.Key + ".png";
                var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                int w = existing != null ? existing.width : 26;
                int h = existing != null ? existing.height : 26;

                string legacyPath = "Assets/Art/Legacy/" + entry.Key + ".png";
                if (existing != null && !File.Exists(legacyPath))
                    File.Copy(path, legacyPath);

                Begin(w, h);
                entry.Value();
                var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
                tex.SetPixels32(px);
                tex.Apply();
                File.WriteAllBytes(path, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                forged++;
            }

            AssetDatabase.Refresh();
            foreach (var entry in Map)
            {
                string path = "Assets/Art/" + entry.Key + ".png";
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                if (importer == null) continue;
                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = 32;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
            Debug.Log($"SpriteForge: {forged} sprites forged, legacy copies in Assets/Art/Legacy.");
        }
    }
}
#endif
