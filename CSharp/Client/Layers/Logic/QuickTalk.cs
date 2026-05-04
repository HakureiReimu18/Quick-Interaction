using System;
using System.Reflection;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.IO;

using Barotrauma;
using HarmonyLib;
using Microsoft.Xna.Framework;
using QIDependencyInjection;

namespace QuickInteractions
{
  [Singleton]
  public class QuickTalk
  {
    [Dependency] public CustomInteractionsTracker CustomInteractionsTracker { get; set; }
    [Dependency] public FakeInput FakeInput { get; set; }
    public event Action<Character> CharacterStatusUpdated;

    public IEnumerable<Character> Interactable => Character.CharacterList.Where(
      character => character.CampaignInteractionType != CampaignMode.InteractionType.None
    );

    public IEnumerable<Character> WantToTalk => Character.CharacterList.Where(character =>
      character.CampaignInteractionType == CampaignMode.InteractionType.Talk ||
      character.CampaignInteractionType == CampaignMode.InteractionType.Examine
    );

    public IEnumerable<Character> Merchants => Character.CharacterList.Where(character =>
      character.CampaignInteractionType == CampaignMode.InteractionType.Crew ||
      character.CampaignInteractionType == CampaignMode.InteractionType.Store ||
      character.CampaignInteractionType == CampaignMode.InteractionType.Upgrade ||
      character.CampaignInteractionType == CampaignMode.InteractionType.PurchaseSub ||
      character.CampaignInteractionType == CampaignMode.InteractionType.MedicalClinic
    );

    public void InteractWith(Character character)
    {
      if (character == null) return;
      if (Character.Controlled == null) return;
      if (character.IsDead) return;

      if (character.onCustomInteract != null)
      {
        try
        {
          if (GameMain.IsMultiplayer)
          {
            FakeInput.StartRemoteInteraction(character.ID);
            FakeInput.SendInteractPackage(character);
          }

          character.onCustomInteract(character, Character.Controlled);
          ScheduleCharacterUpdate(character);
        }
        finally
        {
          if (GameMain.IsMultiplayer)
          {
            FakeInput.EndRemoteInteraction(character.ID);
          }
        }
      }
      else
      {
        CharacterStatusUpdated?.Invoke(character);
      }
    }

    public void ScheduleCharacterUpdate(Character character, int delay = 200)
    {
      LuaCsSetup.Instance.Timer.Wait((object[] args) => CharacterStatusUpdated?.Invoke(character), delay);
    }

    public void AfterInject()
    {
      CustomInteractionsTracker.OnCharacterCreated += (c) => ScheduleCharacterUpdate(c);
      CustomInteractionsTracker.OnCharacterKilled += (c) => ScheduleCharacterUpdate(c);
      CustomInteractionsTracker.OnCharacterDespawned += (c) => ScheduleCharacterUpdate(c);
      CustomInteractionsTracker.OnCustomInteractSet += (c) => ScheduleCharacterUpdate(c);
      CustomInteractionsTracker.OnConversationEnded += (c) => ScheduleCharacterUpdate(c);
    }
  }
}