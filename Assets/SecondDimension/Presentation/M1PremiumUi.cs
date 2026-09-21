using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SecondDimension.Presentation
{
    /// <summary>
    /// Native-uGUI production skin for the M1 proof. Every texture is generated from
    /// fixed constants, cached, and used for presentation only. No generated value is
    /// written to campaign state or participates in a canonical hash.
    /// </summary>
    internal static class M1PremiumUi
    {
        internal enum Surface
        {
            Iron,
            EtchedGlass,
            Vellum,
            Parchment,
            Positive,
            Warning,
            Error,
            HighContrast,
            WorldGlass,
            WorldPaper,
            WorldRibbon
        }

        internal enum ButtonRole
        {
            Primary,
            Selected,
            Warning,
            Destructive
        }

        public const string KitVersion = "M1_LOCATION_FIRST_UI_1.4_SOFT_SURFACES";
        public const string BuiltInFontResource = "LegacyRuntime.ttf";

        private static readonly Dictionary<string, Sprite> Sprites =
            new Dictionary<string, Sprite>(StringComparer.Ordinal);

        private static Font _uiFont;
        private static Font _displayFont;

        public static Font UiFont => _uiFont ?? (_uiFont = ResolveBuiltInFont());

        public static Font DisplayFont => _displayFont ?? (_displayFont = UiFont);

        // Reusable mask for authored artwork. Slice borders keep the corner radius
        // stable at different card sizes, without importing another art asset.
        public static Sprite RoundedMask091 => GetRoundedSurfaceSprite(
            "rounded-art-mask-091", Color.white, Color.clear, false, 10, 0);

        private static Font ResolveBuiltInFont()
        {
            var font = Resources.GetBuiltinResource<Font>(BuiltInFontResource);
            if (font == null)
                throw new InvalidOperationException(
                    "Unity built-in runtime font is unavailable: " + BuiltInFontResource);
            return font;
        }

        public static void StylePage(Image page, bool highContrast)
        {
            StylePanel(page, highContrast ? Surface.HighContrast : Surface.WorldGlass);
            // The authored location is the primary surface. This root only provides
            // edge readability; individual decision areas add their own local material.
            page.color = new Color(1f, 1f, 1f, highContrast ? 0.95f : 0.12f);
            AddFrame(page.transform, "Premium Page Brass Edge", new Color(0.73f, 0.58f, 0.29f, 0.30f), 2f);
        }

        public static void StylePanel(Image image, Surface surface)
        {
            if (image == null) return;
            var colors = SurfaceColors(surface);
            image.sprite = GetRoundedSurfaceSprite("surface-" + surface, colors.fill, colors.edge, false, 9, 2);
            image.type = Image.Type.Sliced;
            image.color = Color.white;
            var worldSurface = surface == Surface.WorldGlass ||
                               surface == Surface.WorldPaper ||
                               surface == Surface.WorldRibbon;
            AddFrame(image.transform, "Premium Surface Edge", colors.edge, worldSurface ? 1f : 2f);

            if (surface == Surface.WorldRibbon)
            {
                AddRail(image.transform, "Premium World Ribbon Mark", true, colors.edge, 3f);
            }

            if (surface == Surface.Vellum || surface == Surface.Parchment || surface == Surface.WorldPaper)
            {
                AddCornerMark(image.transform, "Premium Ink Corner TL", new Vector2(0f, 1f), new Vector2(1f, -1f));
                AddCornerMark(image.transform, "Premium Ink Corner BR", new Vector2(1f, 0f), new Vector2(-1f, 1f));
            }
        }

        public static void StyleButton(Button button, ButtonRole role)
        {
            if (button == null) return;
            var image = button.targetGraphic as Image ?? button.GetComponent<Image>();
            if (image == null) return;

            image.sprite = GetRoundedSurfaceSprite(
                "button-base",
                new Color(0.93f, 0.95f, 0.97f, 1f),
                new Color(0.72f, 0.78f, 0.85f, 0.30f),
                false,
                10,
                1);
            image.type = Image.Type.Sliced;
            image.color = Color.white;

            var selected = role == ButtonRole.Selected;
            var positive = role == ButtonRole.Primary && button.colors.normalColor == RuntimeUi.Positive;
            if (selected)
            {
                // Accent-backed buttons use a dark ink label. Keep every focused
                // state on the same warm-gold family so keyboard/controller focus
                // can never turn the surface blue underneath that dark label.
                var colors = button.colors;
                colors.normalColor = RuntimeUi.Accent;
                colors.highlightedColor = RuntimeUi.Warning;
                colors.selectedColor = RuntimeUi.Warning;
                colors.pressedColor = new Color(0.63f, 0.44f, 0.17f, 1f);
                button.colors = colors;
            }
            else if (positive)
            {
                // Ascension/recruit/support actions need readable ink on every
                // green state, including keyboard focus and a held press.
                var colors = button.colors;
                colors.normalColor = RuntimeUi.Positive;
                colors.highlightedColor = new Color(0.64f, 0.91f, 0.74f, 1f);
                colors.selectedColor = colors.highlightedColor;
                colors.pressedColor = new Color(0.43f, 0.70f, 0.52f, 1f);
                colors.disabledColor = new Color(0.43f, 0.54f, 0.47f, 0.75f);
                button.colors = colors;
            }
            var roleEdge = positive ? RuntimeUi.Positive : selected
                ? new Color(1f, 0.82f, 0.38f, 1f)
                : role == ButtonRole.Destructive
                    ? RuntimeUi.Error
                    : role == ButtonRole.Warning
                        ? RuntimeUi.Warning
                        : new Color(0.73f, 0.58f, 0.29f, 0.72f);
            AddFrame(
                button.transform,
                role == ButtonRole.Destructive
                    ? "Premium Destructive Edge"
                    : role == ButtonRole.Warning
                        ? "Premium Warning Edge"
                        : selected
                            ? "Premium Selected Brass Edge"
                            : "Premium Button Brass Edge",
                roleEdge,
                selected || role == ButtonRole.Destructive ? 3f : 1f);
            AddRail(
                button.transform,
                selected ? "Premium Selected Gate Mark" : "Premium Gate Mark",
                true,
                role == ButtonRole.Destructive
                    ? RuntimeUi.Error
                    : selected
                        ? new Color(0.94f, 0.76f, 0.31f, 1f)
                        : new Color(0.26f, 0.72f, 0.82f, 0.76f),
                3f);

            var buttonColors = button.colors;
            buttonColors.fadeDuration = 0.12f;
            button.colors = buttonColors;
            AddSoftShadow091(image);
            var feedback = button.GetComponent<M1ButtonFeedback091>();
            if (feedback == null) feedback = button.gameObject.AddComponent<M1ButtonFeedback091>();
            feedback.Configure091(button, RoundedMask091);

            var label = M1ButtonFeedback091.FindLabel091(button)?.GetComponent<Text>();
            if (label != null)
            {
                label.font = UiFont;
                label.color = selected || positive ? new Color(0.07f, 0.08f, 0.09f, 1f) : RuntimeUi.Text;
            }
        }

        public static void StyleInput(InputField input)
        {
            if (input == null) return;
            var image = input.targetGraphic as Image ?? input.GetComponent<Image>();
            if (image == null) return;
            image.sprite = GetRoundedSurfaceSprite(
                "input",
                new Color(0.055f, 0.075f, 0.095f, 0.98f),
                new Color(0.70f, 0.57f, 0.31f, 0.92f),
                false,
                7,
                4);
            image.type = Image.Type.Sliced;
            image.color = Color.white;
            AddFrame(input.transform, "Premium Input Brass Edge", new Color(0.73f, 0.58f, 0.29f, 0.48f), 2f);
            AddSoftShadow091(image);
            if (input.textComponent != null) input.textComponent.font = UiFont;
        }

        public static void StylePortraitFrame(Image frame, bool selected = false)
        {
            if (frame == null) return;
            frame.sprite = GetRoundedSurfaceSprite(
                "portrait-frame-" + (selected ? "selected" : "normal"),
                new Color(0.035f, 0.045f, 0.055f, 0.98f),
                selected ? new Color(0.95f, 0.75f, 0.28f, 1f) : new Color(0.67f, 0.52f, 0.27f, 0.96f),
                false,
                10,
                selected ? 7 : 5);
            frame.type = Image.Type.Sliced;
            frame.color = Color.white;
            AddFrame(frame.transform, "Premium Portrait Brass Edge", selected ? RuntimeUi.Warning : RuntimeUi.Accent, selected ? 8f : 5f);
        }

        public static void StyleMemberCaptionRail076(Image rail)
        {
            if (rail == null) return;
            rail.sprite = GetRoundedSurfaceSprite(
                "union-member-caption-rail-076",
                new Color(0.010f, 0.026f, 0.050f, 0.985f),
                new Color(0.36f, 0.68f, 0.78f, 0.88f),
                false,
                8,
                3);
            rail.type = Image.Type.Sliced;
            rail.color = Color.white;
            rail.raycastTarget = false;
            AddFrame(
                rail.transform,
                "Premium Union Member Caption Rail Edge 076",
                new Color(0.55f, 0.76f, 0.82f, 0.72f),
                3f);
        }

        public static void StyleLocationHotspot(Button button, string glyph, string eyebrow, bool selected = false)
        {
            if (button == null) return;
            var image = button.targetGraphic as Image ?? button.GetComponent<Image>();
            if (image != null)
            {
                image.sprite = GetRoundedSurfaceSprite(
                    selected ? "world-hotspot-selected" : "world-hotspot",
                    selected ? new Color(0.12f, 0.10f, 0.055f, 0.90f) : new Color(0.025f, 0.045f, 0.055f, 0.68f),
                    selected ? RuntimeUi.Warning : new Color(0.76f, 0.61f, 0.31f, 0.78f),
                    false,
                    10,
                    selected ? 6 : 3);
                image.type = Image.Type.Sliced;
                image.color = Color.white;
            }

            var label = button.transform.Find("Label")?.GetComponent<Text>();
            if (label != null)
            {
                Anchor(label.rectTransform, new Vector2(0.05f, 0.04f), new Vector2(0.95f, 0.45f), Vector2.zero, Vector2.zero);
                label.fontSize = 42;
                label.color = RuntimeUi.Text;
            }

            var mark = RuntimeUi.AddText(
                button.transform,
                "World Hotspot Glyph",
                string.IsNullOrWhiteSpace(glyph) ? "◆" : glyph,
                76,
                TextAnchor.MiddleCenter,
                selected ? RuntimeUi.Warning : RuntimeUi.Accent,
                FontStyle.Bold);
            IgnoreLayout(mark.gameObject);
            Anchor(mark.rectTransform, new Vector2(0.10f, 0.44f), new Vector2(0.90f, 0.90f), Vector2.zero, Vector2.zero);
            mark.raycastTarget = false;

            var context = RuntimeUi.AddText(
                button.transform,
                "World Hotspot Context",
                eyebrow ?? string.Empty,
                28,
                TextAnchor.MiddleCenter,
                RuntimeUi.MutedText,
                FontStyle.Bold);
            IgnoreLayout(context.gameObject);
            Anchor(context.rectTransform, new Vector2(0.07f, 0.37f), new Vector2(0.93f, 0.52f), Vector2.zero, Vector2.zero);
            context.raycastTarget = false;
        }

        public static void StyleDossierCard(Button button, bool selected)
        {
            if (button == null) return;
            var image = button.targetGraphic as Image ?? button.GetComponent<Image>();
            if (image != null)
            {
                image.sprite = GetRoundedSurfaceSprite(
                    selected ? "dossier-selected" : "dossier",
                    selected ? new Color(0.34f, 0.245f, 0.12f, 0.74f) : new Color(0.22f, 0.15f, 0.075f, 0.34f),
                    selected ? RuntimeUi.Warning : new Color(0.46f, 0.31f, 0.15f, 0.54f),
                    false,
                    8,
                    selected ? 6 : 2);
                image.type = Image.Type.Sliced;
                image.color = Color.white;
            }

            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.93f, 0.72f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.pressedColor = new Color(0.82f, 0.70f, 0.48f, 1f);
            button.colors = colors;

            var label = button.transform.Find("Label")?.GetComponent<Text>();
            if (label != null)
            {
                label.color = new Color(0.97f, 0.91f, 0.77f, 1f);
                label.fontSize = 38;
            }
        }

        public static void AddSectionDivider(Transform parent, string label, string glyph = "◆")
        {
            var row = new GameObject("Premium Divider " + label, typeof(RectTransform), typeof(LayoutElement));
            row.transform.SetParent(parent, false);
            RuntimeUi.SetLayout(row.GetComponent<RectTransform>(), preferredHeight: 62f);

            var left = RuntimeUi.AddPanel(row.transform, "Gate Line Left", new Color(0.30f, 0.78f, 0.88f, 0.72f));
            Anchor(left.rectTransform, new Vector2(0f, 0.47f), new Vector2(0.34f, 0.53f), new Vector2(8f, 0f), Vector2.zero);
            left.raycastTarget = false;
            var right = RuntimeUi.AddPanel(row.transform, "Gate Line Right", new Color(0.30f, 0.78f, 0.88f, 0.72f));
            Anchor(right.rectTransform, new Vector2(0.66f, 0.47f), new Vector2(1f, 0.53f), Vector2.zero, new Vector2(-8f, 0f));
            right.raycastTarget = false;
            var title = RuntimeUi.AddText(
                row.transform,
                "Divider Label",
                glyph + "  " + (label ?? string.Empty) + "  " + glyph,
                RuntimeUi.SmallBodyFontPixels,
                TextAnchor.MiddleCenter,
                RuntimeUi.Accent,
                FontStyle.Bold);
            Anchor(title.rectTransform, new Vector2(0.34f, 0f), new Vector2(0.66f, 1f), Vector2.zero, Vector2.zero);
            title.raycastTarget = false;
        }

        public static void AddEquipmentSlotBadge(Button button, string slotId, string itemGlyph, bool locked, bool legal)
        {
            if (button == null) return;
            var badge = AddBadge(
                button.transform,
                "Premium Equipment Slot Badge",
                string.IsNullOrWhiteSpace(itemGlyph) ? EquipmentGlyph(slotId) : itemGlyph,
                legal ? RuntimeUi.Accent : RuntimeUi.Warning);
            Anchor(badge, new Vector2(0.015f, 0.18f), new Vector2(0.17f, 0.82f), Vector2.zero, Vector2.zero);
            if (locked)
            {
                var lockBadge = AddBadge(button.transform, "Premium Lock Badge", "LOCK", RuntimeUi.Warning);
                Anchor(lockBadge, new Vector2(0.77f, 0.66f), new Vector2(0.98f, 0.94f), Vector2.zero, Vector2.zero);
            }
        }

        public static void AddClassCrest(Button button, string symbol, string className)
        {
            if (button == null) return;
            var designation = M1VisualAssets.ClassColorDesignation(className);
            var crest = AddBadge(
                button.transform,
                "Premium Class Crest " + designation + " " + (className ?? string.Empty),
                string.IsNullOrWhiteSpace(symbol) ? "◆" : symbol,
                ClassColor(className));
            Anchor(crest, new Vector2(0.80f, 0.67f), new Vector2(0.98f, 0.96f), Vector2.zero, Vector2.zero);
        }

        public static Color ClassColor(string classIdentity)
        {
            switch (M1VisualAssets.ClassColorDesignation(classIdentity))
            {
                case "GUARDIAN": return new Color(0.31f, 0.64f, 1.00f, 1f);
                case "WARRIOR": return new Color(0.94f, 0.31f, 0.38f, 1f);
                case "RANGER": return new Color(0.31f, 0.84f, 0.47f, 1f);
                case "ROGUE": return new Color(1.00f, 0.56f, 0.20f, 1f);
                case "MAGE": return new Color(0.72f, 0.42f, 1.00f, 1f);
                case "PRIEST": return new Color(1.00f, 0.88f, 0.54f, 1f);
                default: return new Color(0.78f, 0.82f, 0.88f, 1f);
            }
        }

        public static void AddClassIdentityBadge(Transform parent, string symbol, string className)
        {
            if (parent == null) return;
            var designation = M1VisualAssets.ClassColorDesignation(className);
            var badge = AddBadge(
                parent,
                "Premium Class Identity " + designation,
                (string.IsNullOrWhiteSpace(symbol) ? "◆" : symbol) + "  " + designation,
                ClassColor(className));
            Anchor(badge, new Vector2(0.52f, 0.80f), new Vector2(0.97f, 0.97f), Vector2.zero, Vector2.zero);
        }

        public static void AddClassColorLegend(Transform parent)
        {
            var row = new GameObject("Premium Class Color Legend", typeof(RectTransform), typeof(LayoutElement));
            row.transform.SetParent(parent, false);
            RuntimeUi.SetLayout(row.GetComponent<RectTransform>(), preferredHeight: 126f);
            RuntimeUi.AddHorizontalLayout(row.GetComponent<RectTransform>(), new RectOffset(0, 0, 0, 0), 12f);

            var classes = new[]
            {
                new[] { "GUARDIAN", "◈", "BLUE" },
                new[] { "WARRIOR", "⚔", "RED" },
                new[] { "RANGER", "➶", "GREEN" },
                new[] { "ROGUE", "◇", "ORANGE" },
                new[] { "MAGE", "✦", "PURPLE" },
                new[] { "PRIEST", "✚", "IVORY GOLD" }
            };
            foreach (var classEntry in classes)
            {
                var edge = ClassColor(classEntry[0]);
                var panel = RuntimeUi.AddPanel(row.transform, "Class Color Legend " + classEntry[0], Color.white);
                RuntimeUi.SetLayout(panel, preferredHeight: 118f, flexibleWidth: 1f);
                panel.sprite = GetRoundedSurfaceSprite(
                    "class-legend-" + classEntry[0],
                    new Color(0.025f, 0.04f, 0.055f, 0.64f),
                    edge,
                    false,
                    8,
                    5);
                panel.type = Image.Type.Sliced;
                RuntimeUi.AddVerticalLayout(panel.transform, new RectOffset(8, 8, 6, 6), 0f, TextAnchor.MiddleCenter);
                RuntimeUi.AddText(panel.transform, "Class Symbol", classEntry[1], 34, TextAnchor.MiddleCenter, edge, FontStyle.Bold);
                RuntimeUi.AddText(panel.transform, "Class Name", classEntry[0], 25, TextAnchor.MiddleCenter, edge, FontStyle.Bold);
                RuntimeUi.AddText(panel.transform, "Class Color Name", classEntry[2], 20, TextAnchor.MiddleCenter, RuntimeUi.MutedText, FontStyle.Bold);
            }
        }

        public static void AddItemChoiceGlyph(Button button, string glyph, bool equipped)
        {
            if (button == null) return;
            var badge = AddBadge(
                button.transform,
                equipped ? "Premium Equipped Item Glyph" : "Premium Item Glyph",
                string.IsNullOrWhiteSpace(glyph) ? "◇" : glyph,
                equipped ? RuntimeUi.Positive : RuntimeUi.Accent);
            Anchor(badge, new Vector2(0.02f, 0.16f), new Vector2(0.18f, 0.84f), Vector2.zero, Vector2.zero);
        }

        public static Color EquipmentRarityColor(string rarityTierId)
        {
            switch ((rarityTierId ?? string.Empty).Trim().ToUpperInvariant())
            {
                case "COMMON": return new Color(0.20f, 0.56f, 1.00f, 1f);
                case "RARE": return new Color(0.69f, 0.32f, 0.96f, 1f);
                case "LEGENDARY": return new Color(1.00f, 0.76f, 0.18f, 1f);
                case "GODLY": return new Color(0.20f, 1.00f, 0.36f, 1f);
                default: return new Color(0.94f, 0.96f, 1.00f, 1f);
            }
        }

        public static void AddEquipmentArtworkToButton(
            Button button,
            string equipmentVisualId,
            string rarityTierId,
            string rarityDisplayName,
            string fallbackGlyph,
            bool equipped,
            bool locked)
        {
            if (button == null) return;
            AddEquipmentArtFrame(
                button.transform,
                equipmentVisualId,
                rarityTierId,
                rarityDisplayName,
                fallbackGlyph,
                new Vector2(0.015f, 0.10f),
                new Vector2(0.235f, 0.90f),
                compact: true);

            var label = button.transform.Find("Label")?.GetComponent<Text>();
            if (label != null)
            {
                label.alignment = TextAnchor.MiddleLeft;
                label.rectTransform.offsetMin = new Vector2(220f, 12f);
                label.rectTransform.offsetMax = new Vector2(equipped || locked ? -210f : -24f, -12f);
            }

            if (equipped)
            {
                var equippedBadge = AddBadge(button.transform, "Premium Equipped Item Picture Badge", "EQUIPPED", RuntimeUi.Positive);
                Anchor(equippedBadge, new Vector2(0.76f, 0.70f), new Vector2(0.985f, 0.94f), Vector2.zero, Vector2.zero);
            }
            if (locked)
            {
                var lockBadge = AddBadge(button.transform, "Premium Lock Badge", "LOCK", RuntimeUi.Warning);
                Anchor(lockBadge, new Vector2(0.76f, 0.45f), new Vector2(0.985f, 0.68f), Vector2.zero, Vector2.zero);
            }
        }

        public static void AddEquipmentPreview(
            Image previewPanel,
            string equipmentVisualId,
            string rarityTierId,
            string rarityDisplayName,
            string displayName,
            string fallbackGlyph,
            bool equipped)
        {
            if (previewPanel == null) return;
            AddEquipmentArtFrame(
                previewPanel.transform,
                equipmentVisualId,
                rarityTierId,
                rarityDisplayName,
                fallbackGlyph,
                new Vector2(0.035f, 0.08f),
                new Vector2(0.48f, 0.92f),
                compact: false);

            var name = RuntimeUi.AddText(
                previewPanel.transform,
                "Equipment Illustration Name",
                (displayName ?? string.Empty) + (equipped ? "\nEQUIPPED" : string.Empty),
                RuntimeUi.SmallBodyFontPixels,
                TextAnchor.MiddleLeft,
                RuntimeUi.Text,
                FontStyle.Bold);
            IgnoreLayout(name.gameObject);
            Anchor(name.rectTransform, new Vector2(0.53f, 0.32f), new Vector2(0.96f, 0.88f), Vector2.zero, Vector2.zero);

            var tier = RuntimeUi.AddText(
                previewPanel.transform,
                "Equipment Illustration Rarity Label",
                (rarityDisplayName ?? "Basic").ToUpperInvariant() + " EQUIPMENT",
                38,
                TextAnchor.MiddleLeft,
                EquipmentRarityColor(rarityTierId),
                FontStyle.Bold);
            IgnoreLayout(tier.gameObject);
            Anchor(tier.rectTransform, new Vector2(0.53f, 0.10f), new Vector2(0.96f, 0.35f), Vector2.zero, Vector2.zero);
        }

        public static void AddEquipmentRarityLegend(Transform parent)
        {
            var row = new GameObject("Premium Equipment Rarity Legend", typeof(RectTransform), typeof(LayoutElement));
            row.transform.SetParent(parent, false);
            RuntimeUi.SetLayout(row.GetComponent<RectTransform>(), preferredHeight: 170f);
            RuntimeUi.AddHorizontalLayout(row.GetComponent<RectTransform>(), new RectOffset(0, 0, 0, 0), 14f);

            var tiers = new[]
            {
                new[] { "BASIC", "WHITE" },
                new[] { "COMMON", "BLUE" },
                new[] { "RARE", "PURPLE" },
                new[] { "LEGENDARY", "GOLD" },
                new[] { "GODLY", "NEON GREEN" }
            };
            foreach (var tier in tiers)
            {
                var edge = EquipmentRarityColor(tier[0]);
                var panel = RuntimeUi.AddPanel(row.transform, "Equipment Rarity Legend " + tier[0], Color.white);
                RuntimeUi.SetLayout(panel, preferredHeight: 160f, flexibleWidth: 1f);
                panel.sprite = GetRoundedSurfaceSprite(
                    "equipment-legend-" + tier[0],
                    new Color(0.025f, 0.04f, 0.055f, 0.98f),
                    edge,
                    false,
                    8,
                    tier[0] == "GODLY" ? 7 : 5);
                panel.type = Image.Type.Sliced;
                RuntimeUi.AddVerticalLayout(panel.transform, new RectOffset(14, 14, 16, 16), 2f, TextAnchor.MiddleCenter);
                RuntimeUi.AddText(panel.transform, "Rarity Tier", tier[0], 34, TextAnchor.MiddleCenter, edge, FontStyle.Bold);
                RuntimeUi.AddText(panel.transform, "Rarity Color", tier[1], 27, TextAnchor.MiddleCenter, RuntimeUi.MutedText, FontStyle.Bold);
            }
        }

        public static void AddFormationSocketBadge(Button button, int slotIndex, bool leader, bool legal)
        {
            if (button == null) return;
            var badge = AddBadge(
                button.transform,
                "Premium Formation Socket " + (slotIndex + 1),
                leader ? "L" : (slotIndex + 1).ToString(),
                legal ? RuntimeUi.Accent : RuntimeUi.Warning);
            Anchor(badge, new Vector2(0.02f, 0.60f), new Vector2(0.20f, 0.94f), Vector2.zero, Vector2.zero);
        }

        public static void AddLegalitySeal(Transform parent, string name, bool legal, string label)
        {
            var seal = AddBadge(
                parent,
                name,
                (legal ? "✓  " : "!  ") + label,
                legal ? RuntimeUi.Positive : RuntimeUi.Warning);
            Anchor(seal, new Vector2(0.58f, 0.025f), new Vector2(0.97f, 0.15f), Vector2.zero, Vector2.zero);
        }

        public static void AddHeaderOrnament(Transform parent)
        {
            var ornament = new GameObject("Premium Header Ornament", typeof(RectTransform), typeof(LayoutElement));
            ornament.transform.SetParent(parent, false);
            RuntimeUi.SetLayout(ornament.GetComponent<RectTransform>(), preferredHeight: 26f);
            var line = RuntimeUi.AddPanel(ornament.transform, "Luminous Gate Separator", new Color(0.29f, 0.78f, 0.88f, 0.76f));
            Anchor(line.rectTransform, new Vector2(0.14f, 0.42f), new Vector2(0.86f, 0.58f), Vector2.zero, Vector2.zero);
            line.raycastTarget = false;
            var crystal = AddBadge(ornament.transform, "Gate Crystal", "◆", new Color(0.40f, 0.87f, 0.94f, 1f));
            Anchor(crystal, new Vector2(0.47f, -0.15f), new Vector2(0.53f, 1.15f), Vector2.zero, Vector2.zero);
        }

        public static void AddProofMedallion(Transform parent, string name, string glyph, string label, string value, bool positive)
        {
            var panel = RuntimeUi.AddPanel(parent, name, Color.white);
            RuntimeUi.SetLayout(panel, preferredHeight: 160f, flexibleWidth: 1f);
            StylePanel(panel, Surface.WorldGlass);
            RuntimeUi.AddVerticalLayout(panel.transform, new RectOffset(18, 18, 10, 10), 0f, TextAnchor.MiddleCenter);
            RuntimeUi.AddText(
                panel.transform,
                "Medallion Glyph",
                glyph,
                44,
                TextAnchor.MiddleCenter,
                positive ? RuntimeUi.Positive : RuntimeUi.Warning,
                FontStyle.Bold);
            RuntimeUi.AddText(panel.transform, "Medallion Label", label, 28, TextAnchor.MiddleCenter, RuntimeUi.MutedText, FontStyle.Bold);
            RuntimeUi.AddText(panel.transform, "Medallion Value", value, 36, TextAnchor.MiddleCenter, RuntimeUi.Text, FontStyle.Bold);
        }

        public static void AddUnionStatReadout(
            Transform parent,
            int memberCount,
            int sharedAp,
            int cohesionBasisPoints,
            int currentHp,
            int maximumHp,
            int currentMp,
            int maximumMp,
            int attack,
            int magicAttack,
            int defense,
            int agility,
            int will)
        {
            if (parent == null) return;
            var panel = RuntimeUi.AddPanel(parent, "Premium Combined Union Stats", Color.white);
            RuntimeUi.SetLayout(panel, preferredHeight: 500f);
            panel.sprite = GetRoundedSurfaceSprite(
                "combined-union-stats",
                new Color(0.025f, 0.04f, 0.055f, 0.76f),
                RuntimeUi.Accent,
                false,
                9,
                6);
            panel.type = Image.Type.Sliced;
            RuntimeUi.AddVerticalLayout(panel.transform, new RectOffset(28, 28, 18, 18), 7f, TextAnchor.MiddleCenter);
            var title = RuntimeUi.AddText(
                panel.transform,
                "Combined Union Stats Title",
                "LIVE UNION OVERVIEW",
                RuntimeUi.CriticalFontPixels,
                TextAnchor.MiddleCenter,
                RuntimeUi.Accent,
                FontStyle.Bold);
            RuntimeUi.SetLayout(title, preferredHeight: 64f);
            var note = RuntimeUi.AddText(
                panel.transform,
                "Combined Union Stats Note",
                memberCount + " MEMBER" + (memberCount == 1 ? string.Empty : "S") + " • HP / MP CURRENT / MAX • LIVE AP / COHESION PREVIEW",
                42,
                TextAnchor.MiddleCenter,
                RuntimeUi.MutedText,
                FontStyle.Bold);
            RuntimeUi.SetLayout(note, preferredHeight: 46f);

            var topRow = UnionStatRow(panel.transform, "Union Stat Row Primary");
            AddUnionStat(topRow, "HP", currentHp + " / " + maximumHp);
            AddUnionStat(topRow, "MP", currentMp + " / " + maximumMp);
            AddUnionStat(topRow, "AP", sharedAp.ToString());
            AddUnionStat(topRow, "COH", Percent(cohesionBasisPoints));

            var boundary = RuntimeUi.AddText(
                panel.transform,
                "Combined Union Stats Boundary",
                "OPENING FORMATIONS DO NOT CHANGE THE RAW HP / MP / COMBAT TOTALS BELOW",
                40,
                TextAnchor.MiddleCenter,
                RuntimeUi.Warning,
                FontStyle.Bold);
            RuntimeUi.SetLayout(boundary, preferredHeight: 42f);

            var bottomRow = UnionStatRow(panel.transform, "Union Stat Row Secondary");
            AddUnionStat(bottomRow, "ATK", attack.ToString());
            AddUnionStat(bottomRow, "MAG ATK", magicAttack.ToString());
            AddUnionStat(bottomRow, "DEF", defense.ToString());
            AddUnionStat(bottomRow, "AGI", agility.ToString());
            AddUnionStat(bottomRow, "WILL", will.ToString());
        }

        private static RectTransform UnionStatRow(Transform parent, string name)
        {
            var row = new GameObject(name, typeof(RectTransform), typeof(LayoutElement)).GetComponent<RectTransform>();
            row.SetParent(parent, false);
            RuntimeUi.SetLayout(row, preferredHeight: 138f);
            RuntimeUi.AddHorizontalLayout(row, new RectOffset(0, 0, 0, 0), 12f, TextAnchor.MiddleCenter);
            return row;
        }

        private static void AddUnionStat(Transform parent, string label, string value)
        {
            var metric = RuntimeUi.AddPanel(parent, "Union Stat Metric " + label, Color.white);
            RuntimeUi.SetLayout(metric, preferredHeight: 138f, flexibleWidth: 1f);
            metric.sprite = GetRoundedSurfaceSprite(
                "union-stat-" + label,
                new Color(0.04f, 0.065f, 0.085f, 0.72f),
                RuntimeUi.Accent,
                false,
                6,
                3);
            metric.type = Image.Type.Sliced;
            var labelText = RuntimeUi.AddText(
                metric.transform,
                "Union Stat Label",
                label,
                42,
                TextAnchor.MiddleCenter,
                RuntimeUi.MutedText,
                FontStyle.Bold);
            IgnoreLayout(labelText.gameObject);
            Anchor(
                labelText.rectTransform,
                new Vector2(0f, 0.58f),
                new Vector2(1f, 0.92f),
                new Vector2(8f, 0f),
                new Vector2(-8f, 0f));
            labelText.verticalOverflow = VerticalWrapMode.Overflow;

            var valueText = RuntimeUi.AddText(
                metric.transform,
                "Union Stat Value",
                value,
                60,
                TextAnchor.MiddleCenter,
                RuntimeUi.Text,
                FontStyle.Bold);
            IgnoreLayout(valueText.gameObject);
            Anchor(
                valueText.rectTransform,
                new Vector2(0f, 0.08f),
                new Vector2(1f, 0.58f),
                new Vector2(8f, 0f),
                new Vector2(-8f, 0f));
            valueText.verticalOverflow = VerticalWrapMode.Overflow;
        }

        public static void AddFormationEffectReadout(
            Transform parent,
            string formationName,
            string effectSummary,
            int baseCohesionBasisPoints,
            int formationRuleBasisPoints,
            int projectedCohesionBasisPoints)
        {
            if (parent == null) return;
            var panel = RuntimeUi.AddPanel(parent, "Premium Formation Effect Preview", Color.white);
            RuntimeUi.SetLayout(panel, preferredHeight: 270f);
            panel.sprite = GetRoundedSurfaceSprite(
                "formation-effect-preview",
                new Color(0.035f, 0.055f, 0.07f, 0.99f),
                RuntimeUi.Warning,
                false,
                8,
                5);
            panel.type = Image.Type.Sliced;
            RuntimeUi.AddVerticalLayout(panel.transform, new RectOffset(22, 22, 14, 14), 4f, TextAnchor.MiddleCenter);
            RuntimeUi.AddText(
                panel.transform,
                "Formation Effect Title",
                "FORMATION EFFECT — " + (string.IsNullOrWhiteSpace(formationName) ? "NOT CHOSEN" : formationName.ToUpperInvariant()),
                46,
                TextAnchor.MiddleCenter,
                RuntimeUi.Warning,
                FontStyle.Bold);

            var applied = projectedCohesionBasisPoints - baseCohesionBasisPoints;
            var cohesionText = baseCohesionBasisPoints <= 0
                ? "ADD A MEMBER TO PREVIEW AP AND COHESION"
                : "COHESION " + Percent(baseCohesionBasisPoints) + " → " + Percent(projectedCohesionBasisPoints) +
                  " (" + SignedPercent(applied) + " APPLIED" +
                  (applied == formationRuleBasisPoints ? string.Empty : "; " + SignedPercent(formationRuleBasisPoints) + " RULE CAPPED") + ")";
            RuntimeUi.AddText(
                panel.transform,
                "Formation Cohesion Preview",
                cohesionText,
                54,
                TextAnchor.MiddleCenter,
                RuntimeUi.Text,
                FontStyle.Bold);
            RuntimeUi.AddText(
                panel.transform,
                "Formation Effect Summary",
                string.IsNullOrWhiteSpace(effectSummary) ? "NO OPENING EFFECT SUMMARY" : effectSummary,
                40,
                TextAnchor.MiddleCenter,
                RuntimeUi.MutedText,
                FontStyle.Bold);
        }

        private static string Percent(int basisPoints) => (basisPoints / 100f).ToString("0.#") + "%";

        private static string SignedPercent(int basisPoints) =>
            (basisPoints >= 0 ? "+" : string.Empty) + Percent(basisPoints);

        public static void ConfigureDisplayText(Text text)
        {
            if (text == null) return;
            text.font = DisplayFont;
        }

        private static RectTransform AddBadge(Transform parent, string name, string glyph, Color edge)
        {
            var badge = RuntimeUi.AddPanel(parent, name, Color.white);
            badge.sprite = GetRoundedSurfaceSprite(
                "badge-" + ColorUtility.ToHtmlStringRGBA(edge),
                new Color(0.035f, 0.055f, 0.065f, 0.98f),
                edge,
                false,
                8,
                5);
            badge.type = Image.Type.Sliced;
            badge.color = Color.white;
            badge.raycastTarget = false;
            IgnoreLayout(badge.gameObject);
            var mark = RuntimeUi.AddText(badge.transform, "Badge Glyph", glyph, 38, TextAnchor.MiddleCenter, edge, FontStyle.Bold);
            Stretch(mark.rectTransform, 5f);
            mark.raycastTarget = false;
            return badge.rectTransform;
        }

        private static RectTransform AddEquipmentArtFrame(
            Transform parent,
            string equipmentVisualId,
            string rarityTierId,
            string rarityDisplayName,
            string fallbackGlyph,
            Vector2 anchorMin,
            Vector2 anchorMax,
            bool compact)
        {
            var tierId = string.IsNullOrWhiteSpace(rarityTierId) ? "BASIC" : rarityTierId.Trim().ToUpperInvariant();
            var edge = EquipmentRarityColor(tierId);
            var frame = RuntimeUi.AddPanel(parent, "Premium Equipment Rarity Frame " + tierId, Color.white);
            frame.sprite = GetRoundedSurfaceSprite(
                "equipment-frame-" + tierId,
                new Color(0.018f, 0.028f, 0.04f, 0.98f),
                edge,
                false,
                9,
                tierId == "GODLY" ? 8 : 5);
            frame.type = Image.Type.Sliced;
            frame.color = Color.white;
            frame.raycastTarget = false;
            IgnoreLayout(frame.gameObject);
            Anchor(frame.rectTransform, anchorMin, anchorMax, Vector2.zero, Vector2.zero);

            if (M1VisualAssets.TryResolveEquipment(equipmentVisualId, out var sprite, out _))
            {
                var twinBlades = string.Equals(
                    (equipmentVisualId ?? string.Empty).Trim(),
                    "DAGGER",
                    StringComparison.OrdinalIgnoreCase);
                if (twinBlades)
                {
                    // The neutral atlas owns one blade cell. Mirroring it inside
                    // the equipment frame makes the dagger family read as the
                    // recovered Twin Blades instead of an unrelated off-hand shield.
                    var left = RuntimeUi.AddPanel(
                        frame.transform,
                        "Premium Equipment Art DAGGER Left",
                        Color.white);
                    left.sprite = sprite;
                    left.preserveAspect = true;
                    left.raycastTarget = false;
                    Anchor(
                        left.rectTransform,
                        compact ? new Vector2(0.05f, 0.22f) : new Vector2(0.03f, 0.18f),
                        compact ? new Vector2(0.77f, 0.94f) : new Vector2(0.75f, 0.96f),
                        Vector2.zero,
                        Vector2.zero);

                    var right = RuntimeUi.AddPanel(
                        frame.transform,
                        "Premium Equipment Art DAGGER Right",
                        Color.white);
                    right.sprite = sprite;
                    right.preserveAspect = true;
                    right.raycastTarget = false;
                    Anchor(
                        right.rectTransform,
                        compact ? new Vector2(0.23f, 0.22f) : new Vector2(0.25f, 0.18f),
                        compact ? new Vector2(0.95f, 0.94f) : new Vector2(0.97f, 0.96f),
                        Vector2.zero,
                        Vector2.zero);
                    right.rectTransform.localScale = new Vector3(-1f, 1f, 1f);
                }
                else
                {
                    var artwork = RuntimeUi.AddPanel(frame.transform, "Premium Equipment Art " + equipmentVisualId, Color.white);
                    artwork.sprite = sprite;
                    artwork.preserveAspect = true;
                    artwork.raycastTarget = false;
                    Anchor(
                        artwork.rectTransform,
                        compact ? new Vector2(0.10f, 0.22f) : new Vector2(0.08f, 0.18f),
                        compact ? new Vector2(0.90f, 0.94f) : new Vector2(0.92f, 0.96f),
                        Vector2.zero,
                        Vector2.zero);
                }
            }
            else
            {
                var glyph = RuntimeUi.AddText(
                    frame.transform,
                    "Equipment Art Fallback",
                    string.IsNullOrWhiteSpace(fallbackGlyph) ? "◇" : fallbackGlyph,
                    compact ? 50 : 96,
                    TextAnchor.MiddleCenter,
                    edge,
                    FontStyle.Bold);
                IgnoreLayout(glyph.gameObject);
                Anchor(glyph.rectTransform, new Vector2(0.08f, 0.20f), new Vector2(0.92f, 0.94f), Vector2.zero, Vector2.zero);
            }

            var label = RuntimeUi.AddText(
                frame.transform,
                "Equipment Rarity Label " + tierId,
                (rarityDisplayName ?? "Basic").ToUpperInvariant(),
                compact ? 21 : 28,
                TextAnchor.MiddleCenter,
                edge,
                FontStyle.Bold);
            IgnoreLayout(label.gameObject);
            // Narrow equipped-slot frames must fit actual labels such as
            // UNCOMMON, rather than clipping or substituting a lower rarity.
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = compact ? 12 : 18;
            label.resizeTextMaxSize = compact ? 21 : 28;
            Anchor(label.rectTransform, new Vector2(0.04f, 0.02f), new Vector2(0.96f, compact ? 0.24f : 0.20f), Vector2.zero, Vector2.zero);

            AddFrame(
                frame.transform,
                "Premium Equipment Rarity Glow " + tierId,
                new Color(edge.r, edge.g, edge.b, tierId == "GODLY" ? 0.90f : 0.38f),
                tierId == "GODLY" ? 10f : 4f);
            return frame.rectTransform;
        }

        private static void AddFrame(Transform parent, string name, Color color, float thickness)
        {
            if (parent.Find(name) != null) return;
            var frame = RuntimeUi.AddPanel(parent, name, Color.white);
            frame.sprite = GetRoundedSurfaceSprite(
                "frame-" + thickness + "-" + ColorUtility.ToHtmlStringRGBA(color),
                Color.clear,
                color,
                true,
                9,
                Mathf.Max(2, Mathf.RoundToInt(thickness)));
            frame.type = Image.Type.Sliced;
            frame.color = Color.white;
            frame.raycastTarget = false;
            IgnoreLayout(frame.gameObject);
            Stretch(frame.rectTransform, -1f);
            frame.transform.SetAsLastSibling();
        }

        private static void AddRail(Transform parent, string name, bool left, Color color, float width = 7f)
        {
            if (parent.Find(name) != null) return;
            var rail = RuntimeUi.AddPanel(parent, name, color);
            rail.raycastTarget = false;
            IgnoreLayout(rail.gameObject);
            rail.rectTransform.anchorMin = new Vector2(left ? 0f : 1f, 0.17f);
            rail.rectTransform.anchorMax = new Vector2(left ? 0f : 1f, 0.83f);
            rail.rectTransform.pivot = new Vector2(left ? 0f : 1f, 0.5f);
            rail.rectTransform.sizeDelta = new Vector2(width, 0f);
            rail.rectTransform.anchoredPosition = new Vector2(left ? 3f : -3f, 0f);
        }

        private static void AddCornerMark(Transform parent, string name, Vector2 anchor, Vector2 direction)
        {
            var mark = RuntimeUi.AddText(parent, name, "⌁", 34, TextAnchor.MiddleCenter, new Color(0.30f, 0.22f, 0.11f, 0.72f), FontStyle.Bold);
            mark.raycastTarget = false;
            IgnoreLayout(mark.gameObject);
            mark.rectTransform.anchorMin = anchor;
            mark.rectTransform.anchorMax = anchor;
            mark.rectTransform.pivot = anchor;
            mark.rectTransform.sizeDelta = new Vector2(54f, 54f);
            mark.rectTransform.anchoredPosition = new Vector2(direction.x * 8f, direction.y * 8f);
        }

        private static string EquipmentGlyph(string slotId)
        {
            var id = slotId ?? string.Empty;
            if (id.IndexOf("MAIN_HAND", StringComparison.Ordinal) >= 0) return "MH";
            if (id.IndexOf("OFF_HAND", StringComparison.Ordinal) >= 0) return "OH";
            if (id.IndexOf("BODY", StringComparison.Ordinal) >= 0) return "AR";
            if (id.IndexOf("ACCESSORY", StringComparison.Ordinal) >= 0) return "AC";
            if (id.IndexOf("TOOL", StringComparison.Ordinal) >= 0 || id.IndexOf("RELIC", StringComparison.Ordinal) >= 0) return "TR";
            return "EQ";
        }

        private static (Color fill, Color edge) SurfaceColors(Surface surface)
        {
            switch (surface)
            {
                case Surface.Vellum:
                    return (new Color(0.34f, 0.27f, 0.16f, 0.96f), new Color(0.79f, 0.64f, 0.34f, 0.98f));
                case Surface.Parchment:
                    return (new Color(0.25f, 0.20f, 0.13f, 0.97f), new Color(0.68f, 0.51f, 0.27f, 0.96f));
                case Surface.EtchedGlass:
                    return (new Color(0.035f, 0.065f, 0.085f, 0.76f), new Color(0.36f, 0.71f, 0.75f, 0.82f));
                case Surface.Positive:
                    return (new Color(0.045f, 0.18f, 0.12f, 0.97f), RuntimeUi.Positive);
                case Surface.Warning:
                    return (new Color(0.24f, 0.15f, 0.035f, 0.98f), RuntimeUi.Warning);
                case Surface.Error:
                    return (new Color(0.25f, 0.06f, 0.065f, 0.98f), RuntimeUi.Error);
                case Surface.HighContrast:
                    return (new Color(0.005f, 0.005f, 0.008f, 0.98f), new Color(0.95f, 0.95f, 0.88f, 1f));
                case Surface.WorldGlass:
                    return (new Color(0.018f, 0.033f, 0.043f, 0.30f), new Color(0.49f, 0.69f, 0.70f, 0.54f));
                case Surface.WorldPaper:
                    return (new Color(0.20f, 0.145f, 0.075f, 0.70f), new Color(0.75f, 0.59f, 0.30f, 0.72f));
                case Surface.WorldRibbon:
                    return (new Color(0.020f, 0.035f, 0.045f, 0.78f), new Color(0.78f, 0.62f, 0.32f, 0.80f));
                default:
                    return (new Color(0.035f, 0.050f, 0.065f, 0.97f), new Color(0.66f, 0.52f, 0.28f, 0.92f));
            }
        }

        private static void AddSoftShadow091(Image image)
        {
            var shadow = image.GetComponent<Shadow>();
            if (shadow == null) shadow = image.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0.008f, 0.025f, 0.24f);
            shadow.effectDistance = new Vector2(0f, -5f);
            shadow.useGraphicAlpha = true;
        }

        private static Sprite GetRoundedSurfaceSprite(string key, Color fill, Color edge, bool frameOnly, int corner, int border)
        {
            if (Sprites.TryGetValue(key, out var cached) && cached != null) return cached;
            const int size = 96;
            var radius = Mathf.Clamp(corner * 2.4f, 16f, 27f);
            var edgeWidth = border <= 0 ? 0f : Mathf.Clamp(border * 0.45f, 0.8f, 2.4f);
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false, false)
            {
                name = "M1 UI " + key,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };
            var pixels = new Color32[size * size];
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    // A signed distance gives the silhouette a one-pixel soft edge.
                    // The center stretches as a nine-slice, while corner curvature
                    // and line weight stay constant on every screen resolution.
                    var distance = RoundedDistance091(x + 0.5f, y + 0.5f, size, radius);
                    var coverage = Mathf.Clamp01(0.5f - distance);
                    if (coverage <= 0f)
                    {
                        pixels[y * size + x] = new Color32(0, 0, 0, 0);
                        continue;
                    }

                    var innerCoverage = edgeWidth <= 0f
                        ? coverage
                        : Mathf.Clamp01(0.5f - distance - edgeWidth);
                    var edgeCoverage = coverage - innerCoverage;
                    var material = fill * Mathf.Lerp(0.96f, 1.045f, y / (size - 1f));
                    material.a = fill.a;
                    // Avoid grain/noise in flat areas: it made large menus look
                    // mottled and distracted from the character and location art.
                    var color = frameOnly
                        ? edge
                        : Color.Lerp(material, edge, edgeCoverage / coverage);
                    color.a *= frameOnly ? edgeCoverage * 0.68f : coverage;
                    pixels[y * size + x] = color;
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            var sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                100f,
                0u,
                SpriteMeshType.FullRect,
                new Vector4(30f, 30f, 30f, 30f));
            sprite.name = "M1 UI " + key;
            sprite.hideFlags = HideFlags.HideAndDontSave;
            Sprites[key] = sprite;
            return sprite;
        }

        private static float RoundedDistance091(float x, float y, int size, float radius)
        {
            var half = size * 0.5f;
            var q = new Vector2(Mathf.Abs(x - half), Mathf.Abs(y - half)) -
                    new Vector2(half - radius, half - radius);
            var outside = new Vector2(Mathf.Max(q.x, 0f), Mathf.Max(q.y, 0f));
            return outside.magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - radius;
        }

        private static void IgnoreLayout(GameObject gameObject)
        {
            var layout = gameObject.GetComponent<LayoutElement>();
            if (layout == null) layout = gameObject.AddComponent<LayoutElement>();
            layout.ignoreLayout = true;
        }

        private static void Stretch(RectTransform rect, float inset)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
        }

        private static void Anchor(RectTransform rect, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }
    }
}
