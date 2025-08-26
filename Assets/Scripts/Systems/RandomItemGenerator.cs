using System;
using System.Collections.Generic;

namespace LostAndFound.Systems
{
    /// <summary>
    /// ГЕНЕРАТОР РАНДОМНЫХ ПРЕДМЕТОВ - создаёт логичные пары название-описание для черных NPCs
    /// </summary>
    public static class RandomItemGenerator
    {
        [Serializable]
        public class RandomItemData
        {
            public string itemName;
            public string itemDescription;
            public string tag;
        }

        // 40 логичных пар название-описание
        private static readonly RandomItemData[] randomItems = new RandomItemData[]
        {
            // Электроника
            new RandomItemData { itemName = "Смартфон Samsung Galaxy", itemDescription = "Чёрный смартфон с треснувшим экраном. На задней панели есть царапины, но работает нормально.", tag = "Electronics" },
            new RandomItemData { itemName = "Ноутбук Dell Inspiron", itemDescription = "Серебристый ноутбук с английской раскладкой клавиатуры. На крышке есть небольшая вмятина.", tag = "Electronics" },
            new RandomItemData { itemName = "Беспроводные наушники Sony", itemDescription = "Чёрные наушники с шумоподавлением. Левое ухо немного расшатано, но звук хороший.", tag = "Electronics" },
            new RandomItemData { itemName = "Планшет iPad Air", itemDescription = "Белый планшет Apple с защитным стеклом. На углу есть небольшая трещина.", tag = "Electronics" },
            new RandomItemData { itemName = "Игровая консоль PlayStation 5", itemDescription = "Белая приставка с двумя геймпадами. Один джойстик немного залипает.", tag = "Electronics" },

            // Одежда и аксессуары
            new RandomItemData { itemName = "Кожаный кошелёк", itemDescription = "Коричневый кошелёк с отделениями для карт. Внутри есть фотография семьи.", tag = "Clothing" },
            new RandomItemData { itemName = "Золотое кольцо с камнем", itemDescription = "Тонкое кольцо с небольшим бриллиантом. Внутри выгравированы инициалы.", tag = "Jewelry" },
            new RandomItemData { itemName = "Шерстяной шарф", itemDescription = "Красный шарф с бахромой. На одном конце есть небольшая дырка.", tag = "Clothing" },
            new RandomItemData { itemName = "Солнечные очки Ray-Ban", itemDescription = "Чёрные очки с золотистой оправой. На левой дужке есть царапина.", tag = "Clothing" },
            new RandomItemData { itemName = "Серебряная цепочка", itemDescription = "Тонкая цепочка с кулоном в виде сердца. Застёжка немного расшатана.", tag = "Jewelry" },

            // Документы и книги
            new RandomItemData { itemName = "Паспорт гражданина РФ", itemDescription = "Красный паспорт с фотографией. Страницы немного потрёпаны по краям.", tag = "Documents" },
            new RandomItemData { itemName = "Водительское удостоверение", itemDescription = "Пластиковая карточка с правами категории B. Угол немного потрёпан.", tag = "Documents" },
            new RandomItemData { itemName = "Книга 'Война и мир'", itemDescription = "Толстая книга в твёрдом переплёте. На обложке есть следы от кофе.", tag = "Books" },
            new RandomItemData { itemName = "Ежедневник с записями", itemDescription = "Чёрный ежедневник с ручными заметками. Некоторые страницы вырваны.", tag = "Documents" },
            new RandomItemData { itemName = "Студенческий билет", itemDescription = "Зелёная книжечка с фотографией студента. Обложка немного потрёпана.", tag = "Documents" },

            // Ключи и инструменты
            new RandomItemData { itemName = "Связка ключей", itemDescription = "Несколько ключей на металлическом кольце. Есть ключ от машины и квартиры.", tag = "Keys" },
            new RandomItemData { itemName = "Отвертка Phillips", itemDescription = "Крестовая отвертка с красной ручкой. Лезвие немного затупилось.", tag = "Tools" },
            new RandomItemData { itemName = "Ключ от квартиры", itemDescription = "Один ключ с синей биркой. На бирке написано 'Кв. 45'", tag = "Keys" },
            new RandomItemData { itemName = "Молоток", itemDescription = "Небольшой молоток с деревянной ручкой. На рукоятке есть трещина.", tag = "Tools" },
            new RandomItemData { itemName = "Ключ от машины", itemDescription = "Ключ с брелком от Toyota. На брелке есть кнопка сигнализации.", tag = "Keys" },

            // Игрушки и развлечения
            new RandomItemData { itemName = "Плюшевый медведь", itemDescription = "Большой коричневый медведь с бантом на шее. Одно ухо немного оторвано.", tag = "Toys" },
            new RandomItemData { itemName = "Колода карт", itemDescription = "Стандартная колода из 52 карт. Несколько карт помяты по углам.", tag = "Games" },
            new RandomItemData { itemName = "Мяч для футбола", itemDescription = "Оранжевый мяч с белыми полосками. Камера немного спущена.", tag = "Sports" },
            new RandomItemData { itemName = "Настольная игра 'Монополия'", itemDescription = "Коробка с игрой, не все фишки на месте. Деньги немного потрёпаны.", tag = "Games" },
            new RandomItemData { itemName = "Скакалка", itemDescription = "Красная скакалка с пластиковыми ручками. Одна ручка треснула.", tag = "Sports" },

            // Косметика и гигиена
            new RandomItemData { itemName = "Помада MAC", itemDescription = "Красная помада в золотистой упаковке. Колпачок немного поцарапан.", tag = "Cosmetics" },
            new RandomItemData { itemName = "Зубная щётка Oral-B", itemDescription = "Электрическая щётка с зарядным устройством. Щетинки немного изношены.", tag = "Hygiene" },
            new RandomItemData { itemName = "Духи Chanel", itemDescription = "Маленький флакон с золотистой крышкой. На дне есть небольшая трещина.", tag = "Cosmetics" },
            new RandomItemData { itemName = "Расчёска", itemDescription = "Пластиковая расчёска с несколькими сломанными зубчиками.", tag = "Hygiene" },
            new RandomItemData { itemName = "Зеркальце", itemDescription = "Круглое зеркальце в металлической оправе. Угол немного помят.", tag = "Cosmetics" },

            // Еда и напитки
            new RandomItemData { itemName = "Термос с кофе", itemDescription = "Металлический термос с остатками кофе. Крышка немного протекает.", tag = "Food" },
            new RandomItemData { itemName = "Бутылка воды", itemDescription = "Пластиковая бутылка с минеральной водой. Этикетка немного отклеилась.", tag = "Food" },
            new RandomItemData { itemName = "Шоколадный батончик", itemDescription = "Сникерс в помятой обёртке. Шоколад немного подтаял.", tag = "Food" },
            new RandomItemData { itemName = "Бутерброд", itemDescription = "Бутерброд с колбасой в фольге. Хлеб немного засох.", tag = "Food" },
            new RandomItemData { itemName = "Яблоко", itemDescription = "Красное яблоко с небольшим синяком на боку.", tag = "Food" },

            // Разное
            new RandomItemData { itemName = "Зонт", itemDescription = "Чёрный зонт с деревянной ручкой. Одно спица сломана.", tag = "Misc" },
            new RandomItemData { itemName = "Бумажник", itemDescription = "Кожаный бумажник с отделениями для карт. Застёжка не закрывается.", tag = "Misc" },
            new RandomItemData { itemName = "Часы Casio", itemDescription = "Цифровые часы с резиновым ремешком. Экран немного поцарапан.", tag = "Electronics" },
            new RandomItemData { itemName = "Ручка Parker", itemDescription = "Дорогая ручка с золотым пером. Чернила почти закончились.", tag = "Misc" },
            new RandomItemData { itemName = "Блокнот", itemDescription = "Маленький блокнот с записями. Несколько страниц вырваны.", tag = "Documents" }
        };

        /// <summary>
        /// Возвращает случайный предмет из списка
        /// </summary>
        public static RandomItemData GetRandomItem()
        {
            if (randomItems.Length == 0) return null;
            return randomItems[UnityEngine.Random.Range(0, randomItems.Length)];
        }

        /// <summary>
        /// Возвращает случайный предмет с определённым тегом
        /// </summary>
        public static RandomItemData GetRandomItemByTag(string tag)
        {
            List<RandomItemData> itemsWithTag = new List<RandomItemData>();
            foreach (var item in randomItems)
            {
                if (item.tag == tag)
                    itemsWithTag.Add(item);
            }
            
            if (itemsWithTag.Count == 0) return GetRandomItem();
            return itemsWithTag[UnityEngine.Random.Range(0, itemsWithTag.Count)];
        }

        /// <summary>
        /// Возвращает все доступные теги
        /// </summary>
        public static string[] GetAvailableTags()
        {
            HashSet<string> tags = new HashSet<string>();
            foreach (var item in randomItems)
            {
                tags.Add(item.tag);
            }
            
            string[] result = new string[tags.Count];
            tags.CopyTo(result);
            return result;
        }
    }
} 