using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Engine.Models;
using Engine.Factories;
using System.ComponentModel;
using Engine.EventArgs;

namespace Engine.ViewModels
{
    public class GameSession : BaseNotificationClass
    {
        public event EventHandler<GameMessageEventArgs> OnMessageRaised;

        

        private Location _currentLocation;
        private Monster _currentMonster;

        public World CurrentWorld {get; set;}
        public Player CurrentPlayer { get; set; }

        public Location CurrentLocation
        {
            get {return  _currentLocation; }
            set
            {
                _currentLocation = value;
                OnPropertyChanged(nameof(CurrentLocation));
                OnPropertyChanged(nameof(HasLocationToNorth));
                OnPropertyChanged(nameof(HasLocationToSouth));
                OnPropertyChanged(nameof(HasLocationToEast));
                OnPropertyChanged(nameof(HasLocationToWest));

                GivePlayerQuestsAtLocation();
                GetMonsterAtLocation();
            }
        }

        public Monster CurrentMonster
        {
            get { return _currentMonster; }
            set
            {
                _currentMonster = value;

                OnPropertyChanged(nameof(CurrentMonster));
                OnPropertyChanged(nameof(HasMonster));
                

                if(CurrentMonster != null)
                {
                    RaiseMessage("");
                    RaiseMessage($"You see a {CurrentMonster.Name} here!");
                }
            }
        }

        public Weapon CurrentWeapon { get; set; }

        public bool HasLocationToNorth =>
            CurrentWorld.LocationAt(CurrentLocation.XCoordinate, CurrentLocation.YCoordinate + 1) != null;
        
        public bool HasLocationToEast =>
            CurrentWorld.LocationAt(CurrentLocation.XCoordinate + 1, CurrentLocation.YCoordinate) != null; 
        
        public bool HasLocationToSouth =>
            CurrentWorld.LocationAt(CurrentLocation.XCoordinate, CurrentLocation.YCoordinate - 1) != null; 

        public bool HasLocationToWest =>
            CurrentWorld.LocationAt(CurrentLocation.XCoordinate - 1, CurrentLocation.YCoordinate) != null; 
        

        public bool HasMonster => CurrentMonster != null;

        public GameSession()
        {
            CurrentPlayer = new Player
            {
                Name = "Rob",
                CharacterClass = "Fighter",
                HitPoints = 10,
                Gold = 1000000,
                ExperiencePoints = 0,
                Level = 1
            };

            if(!CurrentPlayer.Weapons.Any())
            {
                CurrentPlayer.AddItemToInventory(ItemFactory.CreateGameItem(1001));
            }

            
            CurrentWorld = WorldFactory.CreateWorld();

            CurrentLocation = CurrentWorld.LocationAt(0, -1);

        }

        public void MoveNorth()
        {
            if(HasLocationToNorth)
            {
                CurrentLocation = CurrentWorld.LocationAt(CurrentLocation.XCoordinate, CurrentLocation.YCoordinate + 1);
            }
        }
            

        public void MoveEast()
        {
            if(HasLocationToEast)
            {
                CurrentLocation = CurrentWorld.LocationAt(CurrentLocation.XCoordinate + 1, CurrentLocation.YCoordinate);
            }
        }

        public void MoveSouth()
        {
            if(HasLocationToSouth)
            {
                CurrentLocation = CurrentWorld.LocationAt(CurrentLocation.XCoordinate, CurrentLocation.YCoordinate - 1);
            }
        }

        public void MoveWest()
        {
            if(HasLocationToWest)
            {
                CurrentLocation = CurrentWorld.LocationAt(CurrentLocation.XCoordinate - 1, CurrentLocation.YCoordinate);
            }
        }

        private void GivePlayerQuestsAtLocation()
        {
            foreach(Quest quest in CurrentLocation.QuestsAvailableHere)
            {
                if(!CurrentPlayer.Quests.Any(q => q.PlayerQuest.ID == quest.ID))
                {
                    CurrentPlayer.Quests.Add(new QuestStatus(quest));
                    RaiseMessage("");
                    RaiseMessage($"You recieve the '{quest.Name}' quest:");
                    RaiseMessage($"{quest.Description}");

                    RaiseMessage("");
                    RaiseMessage("Return with: ");
                    foreach(ItemQuantity itemQuantity in quest.ItemsToComplete)
                    {
                        RaiseMessage($"{itemQuantity.Quantity} {ItemFactory.CreateGameItem(itemQuantity.ItemID).Name}");
                    }

                    RaiseMessage("");
                    RaiseMessage("And you will recieve:");
                    RaiseMessage($" {quest.RewardExperiencePoints} experience points");
                    RaiseMessage($" {quest.RewardGold} gold");
                    foreach(ItemQuantity itemQuantity in quest.RewardItems)
                    {
                        RaiseMessage($" {itemQuantity.Quantity} {ItemFactory.CreateGameItem(itemQuantity.ItemID).Name}");
                    }
                }
            }
        }

        private void GetMonsterAtLocation()
        {
            CurrentMonster = CurrentLocation.GetMonster();
        }

        public void AttackCurrentMonster()
        {
            if(CurrentWeapon == null)
            {
                RaiseMessage("You must select a weapon to attack.");
                return;
            }

            //Determine Damage to monster
            int damageToMonster = RandomNumberGenerator.NumberBetween(CurrentWeapon.MinimumDamage, CurrentWeapon.MaximumDamage);

            if(damageToMonster == 0)
            {
                RaiseMessage($"You missed to {CurrentMonster.Name}");
                
            }

            else
            {
                CurrentMonster.HitPoints -= damageToMonster;
                RaiseMessage($"You hit the {CurrentMonster.Name} for {damageToMonster} points.");
            }

            //If monster is killed, collect rewards and loot
            if(CurrentMonster.HitPoints <= 0)
            {
                RaiseMessage("");
                RaiseMessage($"You defeated the {CurrentMonster.Name}");

                CurrentPlayer.ExperiencePoints += CurrentMonster.RewardExperiencePoints;
                RaiseMessage($"You Recieve {CurrentMonster.RewardExperiencePoints} Experience points.");

                CurrentPlayer.Gold += CurrentMonster.RewardGold;
                RaiseMessage($"You recieve {CurrentMonster.RewardGold} in Gold.");

                foreach(ItemQuantity itemQuantity in CurrentMonster.Inventory)
                {
                    GameItem item = ItemFactory.CreateGameItem(itemQuantity.ItemID);
                    CurrentPlayer.AddItemToInventory(item);
                    RaiseMessage($"You recieve {itemQuantity.Quantity} {item.Name}");
                }

                //Get another monster to fight
                GetMonsterAtLocation();
            }

            else
            {
                //If monster is still alive, let the monster attack
                int damageToPlayer = RandomNumberGenerator.NumberBetween(CurrentMonster.MinimumDamage, CurrentMonster.MinimumDamage);

                if(damageToPlayer == 0)
                {
                    RaiseMessage("");
                    RaiseMessage($"The {CurrentMonster.Name} attacks but misses you.");
                }

                else
                {
                    CurrentPlayer.HitPoints -= damageToPlayer;
                    RaiseMessage($"The {CurrentMonster.Name} hits you for {damageToPlayer} points.");
                }

                //If the player is killed, move them back to their home.
                if(CurrentPlayer.HitPoints <= 0)
                {
                    RaiseMessage("");
                    RaiseMessage($"The {CurrentMonster.Name} killed you!");

                    CurrentLocation = CurrentWorld.LocationAt(0, -1); //Players home
                    CurrentPlayer.HitPoints = CurrentPlayer.Level * 10; //Completely heal the player
                }
            }

        }

        private void RaiseMessage(string message)
        {
            OnMessageRaised?.Invoke(this, new GameMessageEventArgs(message));
        }

    }
}
