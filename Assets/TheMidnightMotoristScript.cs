﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;
using System.Text.RegularExpressions;
using System;
using Rnd = UnityEngine.Random;
//using Bingus;
//using Pemus;

public class TheMidnightMotoristScript : MonoBehaviour {

   public KMAudio audio;
   public KMBombInfo bomb;
   public KMSelectable[] buttons;

   public KMSelectable LeftStick;

   public SpriteRenderer GoalLine;

   public Sprite AOneByOnePixelBlackSquare;

   public TextMesh SolveText;

   public GameObject Selector;
   public GameObject LeftStickGO;
   bool CanMoveStick = false;
   Vector3 MousePos = new Vector3(-1000, -1000, -1000);
   bool MoveUp = false;
   bool MoveDown = false; //Have these as two separate bools so no potential funny shenanigans happens
   bool MoveRegister = false;

   bool MaxJoystickDistance = false;

   public GameObject[] Speakers;

   public Sprite[] CarsSpr;
   public SpriteRenderer[] TestCarsRen;
   public Sprite[] TestRoadsSpr;
   public SpriteRenderer[] TestRoadsRen;

   public SpriteRenderer[] SubCarsRen;
   public Sprite[] SubRoadsSpr;
   public SpriteRenderer SubRoadsRen;

   public GameObject TestPhase;
   public GameObject SubPhase;


   private char[] submitOrder = new char[] { 'B', 'P', 'G', 'V', 'W', 'O', 'Y', 'R' };
   private char[] carColors = new char[] { 'R', 'O', 'Y', 'G', 'B', 'V', 'P', 'W' };
   private char[] raceOrder = new char[] { 'R', 'O', 'Y', 'G', 'B', 'V', 'P', 'W' };
   private char[] currentRace = new char[4];
   private int correctCar;
   private int currentSelection;
   private bool playedRace;
   private bool submissionMode;
   private bool animatingRace;

   private float RoadDelay = .1f; //This is the delay that is in between each change of road
   private int RoadIndex = 0; //Index of sprite

   static int moduleIdCounter = 1;
   int moduleId;
   private bool moduleSolved;

   Coroutine RoadGoBrrrrr;
   Coroutine TestGoBrrrrr;
   Coroutine TickGoBrrrrr;

   void Awake () {
      moduleId = moduleIdCounter++;
      foreach (KMSelectable obj in buttons) {
         KMSelectable pressed = obj;
         pressed.OnInteract += delegate () { PressButton(pressed); return false; };
      }

      LeftStick.OnInteract += delegate () { StickPress(); return false; };
      LeftStick.OnInteractEnded += delegate () { StickRelease(); };
   }

   void Start () {
      raceOrder = raceOrder.Shuffle();
      for (int i = 0; i < raceOrder.Length; i++)
         Debug.LogFormat("[The Midnight Motorist #{0}] The {1} car will always lose to {2}, and will always beat {3}", moduleId, raceOrder[i], GetCarsAhead(raceOrder[i]).ToCharArray().Shuffle().Join(""), GetCarsBefore(raceOrder[i]).ToCharArray().Shuffle().Join(""));
      GenerateSubmission(false);
      GenerateRace();
      StartCoroutine(MoveStick(LeftStickGO));
   }

   void GenerateRace () {
      List<char> usedCars = new List<char>();
      while (usedCars.Count != 4) {
         int choice = Rnd.Range(0, raceOrder.Length);
         while (usedCars.Contains(raceOrder[choice]))
            choice = Rnd.Range(0, raceOrder.Length);
         usedCars.Add(raceOrder[choice]);
      }
      for (int i = 0; i < 4; i++)
         currentRace[i] = usedCars[i];
      for (int i = 0; i < 4; i++)
         TestCarsRen[i].sprite = CarsSpr[Array.IndexOf(carColors, currentRace[i])];
   }

   void GenerateSubmission (bool notFirst) {
      submitOrder = submitOrder.Shuffle();
      for (int i = 0; i < 8; i++)
         SubCarsRen[i].sprite = CarsSpr[Array.IndexOf(carColors, submitOrder[i])];
      Debug.LogFormat("[The Midnight Motorist #{0}] The cars in the submit phase from top to bottom are{2}: {1}", moduleId, submitOrder.Join(""), notFirst ? " now" : "");
      int bracket1 = 1;
      int bracket2 = 3;
      int bracket3 = 5;
      int bracket4 = 7;
      int bracket5;
      int bracket6;
      if (GetCarsBefore(submitOrder[0]).Contains(submitOrder[1]))
         bracket1 = 0;
      if (GetCarsBefore(submitOrder[2]).Contains(submitOrder[3]))
         bracket2 = 2;
      if (GetCarsBefore(submitOrder[4]).Contains(submitOrder[5]))
         bracket3 = 4;
      if (GetCarsBefore(submitOrder[6]).Contains(submitOrder[7]))
         bracket4 = 6;
      if (GetCarsBefore(submitOrder[bracket1]).Contains(submitOrder[bracket2]))
         bracket5 = bracket1;
      else
         bracket5 = bracket2;
      if (GetCarsBefore(submitOrder[bracket3]).Contains(submitOrder[bracket4]))
         bracket6 = bracket3;
      else
         bracket6 = bracket4;
      if (GetCarsBefore(submitOrder[bracket5]).Contains(submitOrder[bracket6]))
         correctCar = bracket5;
      else
         correctCar = bracket6;
      Debug.LogFormat("[The Midnight Motorist #{0}] The correct car to select is: {1}", moduleId, submitOrder[correctCar]);
   }

   void PressButton (KMSelectable pressed) {
      if (moduleSolved != true) {
         audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, pressed.transform);
         int index = Array.IndexOf(buttons, pressed);
         if (index == 0) {
            if (!submissionMode) {
               if (!playedRace) {
                  playedRace = true;
                  animatingRace = true;
                  RoadGoBrrrrr = StartCoroutine(ChangeRoad());
                  TestGoBrrrrr = StartCoroutine(ShowTestRace());
               }
               else {
                  playedRace = false;
                  animatingRace = false;
                  StopCoroutine(RoadGoBrrrrr);
                  StopCoroutine(TestGoBrrrrr);
                  RoadDelay = .1f;
                  RoadIndex = 0;
                  TestRoadsRen[0].sprite = TestRoadsSpr[RoadIndex];
                  TestRoadsRen[1].sprite = TestRoadsSpr[RoadIndex];
                  GoalLine.transform.localPosition = new Vector3(-1.25f, 0.458f, -0.252f);
                  TestCarsRen[0].transform.localPosition = new Vector3(0.7f, 0.458f, -0.757f);
                  TestCarsRen[1].transform.localPosition = new Vector3(0.7f, 0.458f, -0.442f);
                  TestCarsRen[2].transform.localPosition = new Vector3(0.7f, 0.458f, -0.127f);
                  TestCarsRen[3].transform.localPosition = new Vector3(0.7f, 0.458f, 0.188f);
                  GenerateRace();
               }
            }
            else {
               if (currentSelection == correctCar) {
                  Debug.LogFormat("[The Midnight Motorist #{0}] You chose the {1} car, which is correct", moduleId, submitOrder[currentSelection]);
                  moduleSolved = true;
               }
               else {
                  Debug.LogFormat("[The Midnight Motorist #{0}] You chose the {1} car, which is incorrect", moduleId, submitOrder[currentSelection]);
               }
               StartCoroutine(ShowFinalRace());
               //Run submit mode race and strike/pass at end
            }
         }
         else if (index == 1 && !animatingRace) {
            submissionMode = !submissionMode;
            if (submissionMode) {
               TestPhase.SetActive(false);
               SubPhase.SetActive(true);
            }
            else {
               TestPhase.SetActive(true);
               SubPhase.SetActive(false);
            }
         }
         else if (index > 1) {
            Debug.LogFormat("[The Midnight Motorist #{0}] You dared to fiddle with Alfonso's controls", moduleId);
            GetComponent<KMBombModule>().HandleStrike();
         }
      }
   }

   string GetCarsAhead (char car) {
      string ahead = "";
      int index = Array.IndexOf(raceOrder, car);
      for (int i = 0; i < (Array.IndexOf(raceOrder, car) < 4 ? 3 : 4); i++) {
         index += 1;
         if (index == raceOrder.Length)
            index = 0;
         ahead += raceOrder[index];
      }
      return ahead;
   }

   string GetCarsBefore (char car) {
      string before = "";
      int index = Array.IndexOf(raceOrder, car);
      for (int i = 0; i < (Array.IndexOf(raceOrder, car) < 4 ? 4 : 3); i++) {
         index -= 1;
         if (index == -1)
            index = raceOrder.Length - 1;
         before += raceOrder[index];
      }
      return before;
   }

   void StickPress () {
      CanMoveStick = true;
   }

   void StickRelease () {
      CanMoveStick = false;
      if (TickGoBrrrrr != null) {
         StopCoroutine(TickGoBrrrrr);
      }
   }

   #region Animation

   IEnumerator ChangeRoad () {
      while (true) {
         if (TestPhase.activeSelf) {
            TestRoadsRen[0].sprite = TestRoadsSpr[RoadIndex];
            TestRoadsRen[1].sprite = TestRoadsSpr[RoadIndex];
         }
         else {
            SubRoadsRen.sprite = SubRoadsSpr[RoadIndex];
         }

         yield return new WaitForSecondsRealtime(RoadDelay);
         RoadIndex = (RoadIndex + 1) % 3;
      }
   }

   /*void RaceResult (SpriteRenderer First, SpriteRenderer Second, SpriteRenderer Third, SpriteRenderer Last) {
      StartCoroutine(MoveCar(First, 4));
      StartCoroutine(MoveCar(Second, 3));
      StartCoroutine(MoveCar(Third, 2));
      StartCoroutine(MoveCar(Last, 1.5));
   }

   IEnumerator MoveCar (SpriteRenderer Spr, double invSpeed) {
      for (int i = 0; i < 25; i++) {
         Spr.transform.transform.transform.transform.transform.transform.transform.localPosition -= new Vector3(.056f, 0, 0);
         yield return new WaitForSeconds((float) .1 / (float) invSpeed);
      }
   }*/

   IEnumerator ShowTestRace () {
      float[] TempSpeeds1 = { Rnd.Range(0.04f, 0.056f), Rnd.Range(0.04f, 0.056f), Rnd.Range(0.04f, 0.056f), Rnd.Range(0.04f, 0.056f) };
      for (int i = 0; i < 20; i++) {
         for (int j = 0; j < 4; j++) {
            TestCarsRen[j].transform.transform.transform.transform.transform.transform.transform.localPosition -= new Vector3(TempSpeeds1[j], 0, 0);
         }
         yield return new WaitForSeconds((float) .1 / 2);
      }
      TempSpeeds1 = new float[] { Rnd.Range(0.01f, 0.03f), Rnd.Range(0.01f, 0.03f), Rnd.Range(0.01f, 0.03f), Rnd.Range(0.01f, 0.03f) };
      for (int i = 0; i < 20; i++) {
         for (int j = 0; j < 4; j++) {

            TestCarsRen[j].transform.transform.transform.transform.transform.transform.transform.localPosition -= new Vector3(TempSpeeds1[j], 0, 0);
         }
         yield return new WaitForSeconds((float) .1 / 2);
      }
      RoadDelay /= 5;
      TempSpeeds1 = new float[] { Rnd.Range(.065f, 0.09f), Rnd.Range(.065f, 0.09f), Rnd.Range(.065f, 0.09f), Rnd.Range(.065f, 0.09f) };
      for (int i = 0; i < 30; i++) {
         for (int j = 0; j < 4; j++) {
            TestCarsRen[j].transform.transform.transform.transform.transform.transform.transform.localPosition += new Vector3(TempSpeeds1[j], 0, 0);
         }
         yield return new WaitForSeconds((float) .1 / 2);
      }
      yield return new WaitForSeconds(.9f);
      for (int i = 0; i < 12; i++) {
         GoalLine.transform.transform.transform.transform.transform.transform.transform.localPosition += new Vector3(.04f, 0, 0);
         yield return new WaitForSeconds(.01f);
      }
      StopCoroutine(RoadGoBrrrrr);
      yield return new WaitForSeconds(.5f);
      for (int j = 0; j < 4; j++) {
         TestCarsRen[j].transform.transform.transform.transform.transform.transform.transform.localPosition = new Vector3(1, 0.458f, TestCarsRen[j].transform.localPosition.z);
      }
      //Make sure race actually displays properly here
      TempSpeeds1 = new float[] { Rnd.Range(.1f, 0.2f), Rnd.Range(.1f, 0.2f), Rnd.Range(.1f, 0.2f), Rnd.Range(.1f, 0.2f) };
      for (int i = 0; i < 30; i++) {
         for (int j = 0; j < 4; j++) {
            TestCarsRen[j].transform.transform.transform.transform.transform.transform.transform.localPosition -= new Vector3(TempSpeeds1[j], 0, 0);
         }
         yield return new WaitForSeconds(.03f);
      }
      animatingRace = false;
   }

   IEnumerator ShowFinalRace () {
      audio.PlaySoundAtTransform("Midnight Motorist Music 1", transform);
      Selector.SetActive(false);
      yield return new WaitForSeconds(1f);
      RoadGoBrrrrr = StartCoroutine(ChangeRoad());
      float[] TempSpeeds1 = { Rnd.Range(0.04f, 0.056f), Rnd.Range(0.04f, 0.056f), Rnd.Range(0.04f, 0.056f), Rnd.Range(0.04f, 0.056f), Rnd.Range(0.04f, 0.056f), Rnd.Range(0.04f, 0.056f), Rnd.Range(0.04f, 0.056f), Rnd.Range(0.04f, 0.056f)};
      for (int i = 0; i < 20; i++) {
         for (int j = 0; j < 8; j++) {
            SubCarsRen[j].transform.transform.transform.transform.transform.transform.transform.localPosition -= new Vector3(TempSpeeds1[j], 0, 0);
         }
         yield return new WaitForSeconds((float) .1 / 2);
      }
      TempSpeeds1 = new float[] { Rnd.Range(0.01f, 0.03f), Rnd.Range(0.01f, 0.03f), Rnd.Range(0.01f, 0.03f), Rnd.Range(0.01f, 0.03f), Rnd.Range(0.01f, 0.03f), Rnd.Range(0.01f, 0.03f), Rnd.Range(0.01f, 0.03f), Rnd.Range(0.01f, 0.03f) };
      for (int i = 0; i < 30; i++) {
         for (int j = 0; j < 8; j++) {

            SubCarsRen[j].transform.transform.transform.transform.transform.transform.transform.localPosition -= new Vector3(TempSpeeds1[j], 0, 0);
         }
         yield return new WaitForSeconds((float) .1 / 2);
      }
      RoadDelay /= 5;
      TempSpeeds1 = new float[] { Rnd.Range(.065f, 0.09f), Rnd.Range(.065f, 0.09f), Rnd.Range(.065f, 0.09f), Rnd.Range(.065f, 0.09f), Rnd.Range(.065f, 0.09f), Rnd.Range(.065f, 0.09f), Rnd.Range(.065f, 0.09f), Rnd.Range(.065f, 0.09f) };
      for (int i = 0; i < 30; i++) {
         for (int j = 0; j < 8; j++) {
            SubCarsRen[j].transform.transform.transform.transform.transform.transform.transform.localPosition += new Vector3(TempSpeeds1[j], 0, 0);
         }
         yield return new WaitForSeconds((float) .1 / 2);
      }
      yield return new WaitForSeconds(.4f);
      for (int i = 0; i < 12; i++) {
         GoalLine.transform.transform.transform.transform.transform.transform.transform.localPosition += new Vector3(.04f, 0, 0);
         yield return new WaitForSeconds(.01f);
      }
      StopCoroutine(RoadGoBrrrrr);
      yield return new WaitForSeconds(1.8f);
      for (int j = 0; j < 8; j++) {
         SubCarsRen[j].transform.transform.transform.transform.transform.transform.transform.localPosition = new Vector3(1, 0.458f, SubCarsRen[j].transform.localPosition.z);
      }
      //Make sure race actually displays properly here
      TempSpeeds1 = new float[] { Rnd.Range(.1f, 0.2f), Rnd.Range(.1f, 0.2f), Rnd.Range(.1f, 0.2f), Rnd.Range(.1f, 0.2f), Rnd.Range(.1f, 0.2f), Rnd.Range(.1f, 0.2f), Rnd.Range(.1f, 0.2f), Rnd.Range(.1f, 0.2f) };
      for (int i = 0; i < 30; i++) {
         for (int j = 0; j < 8; j++) {
            SubCarsRen[j].transform.transform.transform.transform.transform.transform.transform.localPosition -= new Vector3(TempSpeeds1[j], 0, 0);
         }
         yield return new WaitForSeconds(.03f);
      }
      animatingRace = false;
      yield return new WaitForSeconds(.2f);
      SubRoadsRen.sprite = AOneByOnePixelBlackSquare;
      GoalLine.gameObject.SetActive(false);
      yield return new WaitForSeconds(1f);
      SolveText.gameObject.SetActive(true);
   }

   IEnumerator MoveStick (GameObject Stick) {
      while (true) {
         //Debug.Log(Stick.transform.localEulerAngles.x);
         if (MoveUp) {
            if (Stick.transform.localEulerAngles.x < 30f || Stick.transform.localEulerAngles.x > 329f) {
               MoveRegister = false;
               Stick.transform.Rotate(new Vector3(3f, 0, 0));
            }
            else if (!MoveRegister) {
               MoveRegister = true;
               TickGoBrrrrr = StartCoroutine(MoveSelectionTick("up"));
            }
            if (Stick.transform.localEulerAngles.x >= 30f && Stick.transform.localEulerAngles.x < 300f) {
               MaxJoystickDistance = true;
            }
            else {
               MaxJoystickDistance = false;
               if (TickGoBrrrrr != null) {
                  StopCoroutine(TickGoBrrrrr);
               }
            }
         }
         else if (MoveDown) {
            if (Stick.transform.localEulerAngles.x > 330 || Stick.transform.localEulerAngles.x < 31) {
               MoveRegister = false;
               Stick.transform.Rotate(new Vector3(-3f, 0, 0));
            }
            else if (!MoveRegister) {
               MoveRegister = true;
               TickGoBrrrrr = StartCoroutine(MoveSelectionTick("down"));
            }
            if (Stick.transform.localEulerAngles.x <= 330 && Stick.transform.localEulerAngles.x > 40f) {
               MaxJoystickDistance = true;
            }
            else {
               MaxJoystickDistance = false;
               if (TickGoBrrrrr != null) {
                  StopCoroutine(TickGoBrrrrr);
               }
            }
         }
         else if (!MoveDown && !MoveUp) {
            MoveRegister = false;
            if (Stick.transform.localEulerAngles.x > 0 && Stick.transform.localEulerAngles.x < 36) {
               Stick.transform.localEulerAngles += new Vector3(-3f, 0, 0);
               //LeftStickGO.transform.Rotate(new Vector3(-3f, 0, 0));
            }
            else if (Stick.transform.localEulerAngles.x > 300) {
               //LeftStickGO.transform.Rotate(new Vector3(3f, 0, 0));
               Stick.transform.localEulerAngles += new Vector3(3f, 0, 0);
            }
            if (Math.Abs(1 - Stick.transform.localEulerAngles.x) > 0 && Stick.transform.localEulerAngles.x < 1) {
               Stick.transform.localEulerAngles = new Vector3(0, 0, 0);
            }
         }
         yield return new WaitForSeconds(.01f);
      }
   }

   IEnumerator MoveSelectionTick (string dir) {
      while (MaxJoystickDistance) {
         if (dir == "up") {
            currentSelection -= 1;
            if (currentSelection == -1)
               currentSelection = raceOrder.Length - 1;
            Selector.transform.localPosition = new Vector3(0.618f, SubCarsRen[currentSelection].transform.localPosition.y, SubCarsRen[currentSelection].transform.localPosition.z);
         }
         else {
            currentSelection += 1;
            if (currentSelection == raceOrder.Length)
               currentSelection = 0;
            Selector.transform.localPosition = new Vector3(0.618f, SubCarsRen[currentSelection].transform.localPosition.y, SubCarsRen[currentSelection].transform.localPosition.z);
         }
         yield return new WaitForSeconds(.25f);
      }
   }

   #endregion

   private void Update () {
      if (CanMoveStick) {
         if (MousePos == new Vector3(-1000, -1000, -1000)) {
            MousePos = Input.mousePosition;
         }
         if (Input.mousePosition.y > MousePos.y) {
            MoveUp = true;
            MoveDown = false;
         }
         else if (Input.mousePosition.y < MousePos.y) { //elif necessary so that if player does not move cursor, stick does not go up
            MoveDown = true;
            MoveUp = false;
         }
      }
      if (!CanMoveStick) {
         MousePos = new Vector3(-1000, -1000, -1000);
         MoveUp = false;
         MoveDown = false;
      }
   }

   //twitch plays
#pragma warning disable 414
   private readonly string TwitchHelpMessage = @"!{0} something [Does something]";
#pragma warning restore 414
   IEnumerator ProcessTwitchCommand (string command) {
      if (Regex.IsMatch(command, @"^\s*something\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)) {
         yield return null;
         Debug.Log("Did something");
         yield break;
      }
   }
}