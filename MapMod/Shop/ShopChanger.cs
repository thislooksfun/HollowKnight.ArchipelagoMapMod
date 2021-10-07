﻿using System.Collections.Generic;
using MapMod.Data;
using MapMod.Map;
using MapMod.Settings;
using UnityEngine;

// This code was heavily borrowed from RandomizerMod 3.0
namespace MapMod.Shop
{
    public static class ShopChanger
    {
        private static ShopDef[] _shopItems;

        public static void Hook()
        {
            On.PlayMakerFSM.OnEnable += PlayMakerFSM_OnEnable;
        }

        private static void PlayMakerFSM_OnEnable(On.PlayMakerFSM.orig_OnEnable orig, PlayMakerFSM self)
        {
            orig(self);

            if (self.gameObject.scene.name != "Room_mapper" || self.gameObject.name != "Shop Menu")
            {
                return;
            }

            _shopItems = DataLoader.GetShopArray();

            RefreshIseldaShop();
        }

        public static void RefreshIseldaShop()
        {
            foreach (GameObject shopObj in Object.FindObjectsOfType<GameObject>())
            {
                if (shopObj.name != "Shop Menu") continue;

                ShopMenuStock shop = shopObj.GetComponent<ShopMenuStock>();
                GameObject itemPrefab = Object.Instantiate(shop.stock[0]);
                itemPrefab.SetActive(false);

                List<GameObject> newStock = new();

                foreach (GameObject item in shop.stock)
                {// (specialType: 0 = lantern, elegant key, quill; 1 = mask, 2 = charm, 3 = vessel, 4-7 = relics, 8 = notch, 9 = map, 10 = simple key, 11 = egg, 12-14 = repair fragile, 15 = salubra blessing, 16 = map pin, 17 = map marker)
                    // Remove Map Markers from the game
                    if (item.GetComponent<ShopItemStats>().specialType != 17)
                    {
                        newStock.Add(item);
                    }
                }

                foreach (ShopDef shopItem in _shopItems)
                {
                    if (SettingsUtil.GetMapModSettingFromPool(shopItem.pool))
                    {
                        continue;
                    }

                    // Create a new shop item for this item def
                    GameObject newItemObj = Object.Instantiate(itemPrefab);
                    newItemObj.SetActive(false);

                    // Apply all the stored values
                    ShopItemStats stats = newItemObj.GetComponent<ShopItemStats>();
                    stats.playerDataBoolName = shopItem.playerDataBoolName;
                    stats.nameConvo = shopItem.nameConvo;
                    stats.descConvo = shopItem.descConvo;
                    stats.dungDiscount = false;
                    stats.cost = shopItem.cost;

                    // Need to set all these to make sure the item doesn't break in one of various ways
                    stats.priceConvo = string.Empty;
                    stats.specialType = 16;
                    stats.charmsRequired = 0;
                    stats.relic = false;
                    stats.relicNumber = 0;
                    stats.relicPDInt = string.Empty;

                    // Apply the sprite for the UI
                    stats.transform.Find("Item Sprite").gameObject.GetComponent<SpriteRenderer>().sprite =
                        SpriteManager.GetSprite(shopItem.spriteName);
                    //stats.transform.Find("Item Sprite").localScale = new Vector2(1.0f, 1.0f);
                    stats.transform.Find("Item Sprite").localPosition = new Vector2(0.08f, 0.0f);

                    newStock.Add(newItemObj);
                }

                shop.stock = newStock.ToArray();
            }
        }
    }
}