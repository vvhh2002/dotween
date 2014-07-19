﻿// Author: Daniele Giardini - http://www.demigiant.com
// Created: 2014/07/15 17:50

using System.Collections.Generic;
using DG.Tweening.Core;
using DG.Tweening.Core.Enums;
using UnityEngine;

namespace DG.Tweening
{
    public sealed class Sequence : Tween
    {
        // SETUP DATA ////////////////////////////////////////////////

        internal readonly List<Tween> sequencedTweens = new List<Tween>(); // Only Tweens (used for despawning)
        readonly List<ABSSequentiable> _sequencedObjs = new List<ABSSequentiable>(); // Tweens plus SequenceCallbacks

        internal Sequence()
        {
            tweenType = TweenType.Sequence;
            Reset();
        }

        // ===================================================================================
        // PUBLIC METHODS --------------------------------------------------------------------

        public override void Reset()
        {
            base.Reset();

            sequencedTweens.Clear();
            _sequencedObjs.Clear();
        }

        // ===================================================================================
        // CREATION METHODS ------------------------------------------------------------------

        internal static Sequence DoPrepend(Sequence inSequence, Tween t)
        {
            inSequence.duration += t.duration;
            int len = inSequence._sequencedObjs.Count;
            for (int i = 0; i < len; ++i) {
                ABSSequentiable sequentiable = inSequence._sequencedObjs[i];
                sequentiable.sequencedPosition += t.duration;
                sequentiable.sequencedEndPosition += t.duration;
            }

            return DoInsert(inSequence, t, 0);
        }

        internal static Sequence DoInsert(Sequence inSequence, Tween t, float atPosition)
        {
            TweenManager.AddActiveTweenToSequence(t);

            t.isSequenced = t.creationLocked = true;
            if (t.loops == -1) t.loops = 1;
            t.autoKill = false;
            t.delay = t.elapsedDelay = 0;
            t.delayComplete = true;
            t.sequencedPosition = atPosition;
            t.sequencedEndPosition = t.sequencedPosition + (t.duration * t.loops);

            if (t.sequencedEndPosition > inSequence.duration) inSequence.duration = t.sequencedEndPosition;
            inSequence._sequencedObjs.Add(t);
            inSequence.sequencedTweens.Add(t);

            return inSequence;
        }

        internal static Sequence DoAppendInterval(Sequence inSequence, float interval)
        {
            inSequence.duration += interval;
            return inSequence;
        }

        internal static Sequence DoPrependInterval(Sequence inSequence, float interval)
        {
            inSequence.duration += interval;
            int len = inSequence._sequencedObjs.Count;
            for (int i = 0; i < len; ++i) {
                ABSSequentiable sequentiable = inSequence._sequencedObjs[i];
                sequentiable.sequencedPosition += interval;
                sequentiable.sequencedEndPosition += interval;
            }

            return inSequence;
        }

        internal static Sequence DoInsertCallback(Sequence inSequence, TweenCallback callback, float atPosition)
        {
            SequenceCallback c = new SequenceCallback(atPosition, callback);
            c.sequencedPosition = c.sequencedEndPosition = atPosition;
            inSequence._sequencedObjs.Add(c);
            if (inSequence.duration < atPosition) inSequence.duration = atPosition;
            return inSequence;
        }

        // ===================================================================================
        // INTERNAL METHODS ------------------------------------------------------------------

        // CALLED BY Tween the moment the tween starts.
        // Returns TRUE in case of success (always TRUE for Sequences)
        internal override bool Startup()
        {
            return DoStartup(this);
        }

        internal override bool ApplyTween(float prevPosition, int prevCompletedLoops, int newCompletedSteps, bool useInversePosition, UpdateMode updateMode)
        {
            return DoApplyTween(this, prevPosition, prevCompletedLoops, newCompletedSteps, useInversePosition, updateMode);
        }

        // Called by DOTween when spawning/creating a new Sequence.
        internal static void Setup(Sequence s)
        {
            s.isPlaying = DOTween.defaultAutoPlayBehaviour == AutoPlay.All || DOTween.defaultAutoPlayBehaviour == AutoPlay.AutoPlaySequences;
            s.loopType = DOTween.defaultLoopType;
        }

        internal static bool DoStartup(Sequence s)
        {
            s.startupDone = true;
            s.fullDuration = s.loops > -1 ? s.duration * s.loops : Mathf.Infinity;
            // Order sequencedObjs by start position
            s._sequencedObjs.Sort(SortSequencedObjs);
            return true;
        }

        // Applies the tween set by DoGoto.
        // Returns TRUE if the tween needs to be killed
        internal static bool DoApplyTween(Sequence s, float prevPosition, int prevCompletedLoops, int newCompletedSteps, bool useInversePosition, UpdateMode updateMode)
        {
            float from, to = 0;
            if (updateMode == UpdateMode.Update && newCompletedSteps > 0) {
                // Run all cycles elapsed since last update
                int cycles = newCompletedSteps;
                int cyclesDone = 0;
                from = prevPosition;
                bool isInverse = s.loopType == LoopType.Yoyo
                    && (prevPosition < s.duration ? prevCompletedLoops % 2 != 0 : prevCompletedLoops % 2 == 0);
                if (s.isBackwards) isInverse = !isInverse; // TEST
                while (cyclesDone < cycles) {
//                    Debug.Log("::::::::::::: CYCLING : " + s.stringId + " : " + cyclesDone + " ::::::::::::::::::::::::::::::::::::");
                    if (cyclesDone > 0) from = to;
                    else if (isInverse && !s.isBackwards) from = s.duration - from;
                    to = isInverse ? 0 : s.duration;
                    if (ApplyInternalCycle(s, from, to, updateMode)) return true;
                    cyclesDone++;
                    if (s.loopType == LoopType.Yoyo) isInverse = !isInverse;
                }
            }
            // Run current cycle
//            Debug.Log("::::::::::::: UPDATING");
            if (newCompletedSteps > 0) from = useInversePosition ? s.duration : 0;
            else from = useInversePosition ? s.duration - prevPosition : prevPosition;
            return ApplyInternalCycle(s, from, useInversePosition ? s.duration - s.position : s.position, updateMode);
        }

        // ===================================================================================
        // METHODS ---------------------------------------------------------------------------

        static bool ApplyInternalCycle(Sequence s, float fromPos, float toPos, UpdateMode updateMode)
        {
            bool isGoingBackwards = toPos < fromPos;
            if (isGoingBackwards) {
                int len = s._sequencedObjs.Count - 1;
                for (int i = len; i > -1; --i) {
                    ABSSequentiable sequentiable = s._sequencedObjs[i];
                    if (updateMode == UpdateMode.Update && (sequentiable.sequencedEndPosition < toPos || sequentiable.sequencedPosition > fromPos)) continue;
                    if (sequentiable.tweenType == TweenType.Callback) sequentiable.onStart();
                    else {
                        // Nested Tweener/Sequence
                        float gotoPos = toPos - sequentiable.sequencedPosition;
                        if (gotoPos < 0) gotoPos = 0;
                        Tween t = (Tween)sequentiable;
                        t.isBackwards = true;
//                        Debug.Log("             < " + t.stringId + " " + fromPos + "/" + toPos + " : " + gotoPos);
                        if (TweenManager.Goto(t, gotoPos, false, updateMode)) return true;
                    }
                }
            } else {
                int len = s._sequencedObjs.Count;
                for (int i = 0; i < len; ++i) {
                    ABSSequentiable sequentiable = s._sequencedObjs[i];
                    if (updateMode == UpdateMode.Update && (sequentiable.sequencedPosition > toPos || sequentiable.sequencedEndPosition < fromPos)) continue;
                    if (sequentiable.tweenType == TweenType.Callback) sequentiable.onStart();
                    else {
                        // Nested Tweener/Sequence
                        float gotoPos = toPos - sequentiable.sequencedPosition;
                        if (gotoPos < 0) gotoPos = 0;
                        Tween t = (Tween)sequentiable;
                        t.isBackwards = false;
//                        Debug.Log("             > " + t.stringId + " " + fromPos + "/" + toPos + " : " + gotoPos);
                        if (TweenManager.Goto(t, gotoPos, false, updateMode)) return true;
                    }
                }
            }
            return false;
        }

        static int SortSequencedObjs(ABSSequentiable a, ABSSequentiable b)
        {
            if (a.sequencedPosition > b.sequencedPosition) return 1;
            if (a.sequencedPosition < b.sequencedPosition) return -1;
            return 0;
        }
    }
}