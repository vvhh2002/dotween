﻿// Author: Daniele Giardini - http://www.demigiant.com
// Created: 2014/05/07 14:05
// 
// License Copyright (c) Daniele Giardini.
// This work is subject to the terms at http://dotween.demigiant.com/license.php

using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening.Core;
using DG.Tweening.Core.Enums;
using DG.Tweening.Plugins.Core;
using DG.Tweening.Plugins.Options;
using UnityEngine;

namespace DG.Tweening
{
    /// <summary>
    /// Main DOTween class. Contains static methods to create and control tweens in a generic way
    /// </summary>
    public class DOTween
    {
        /// <summary>DOTween's version</summary>
        public static readonly string Version = "0.9.150";

        ///////////////////////////////////////////////
        // Options ////////////////////////////////////

        /// <summary>If TRUE (default) makes tweens slightly slower but safer, automatically taking care of a series of things
        /// (like targets becoming null while a tween is playing).
        /// <para>Default: TRUE</para></summary>
        public static bool useSafeMode = true;
        /// <summary>If TRUE you will get a DOTween report when exiting play mode (only in the Editor).
        /// Useful to know how many max Tweeners and Sequences you reached and optimize your final project accordingly.
        /// Beware, this will slightly slow down your tweens while inside Unity Editor.
        /// <para>Default: FALSE</para></summary>
        public static bool showUnityEditorReport = false;
        /// <summary>Global DOTween timeScale.
        /// <para>Default: 1</para></summary>
        public static float timeScale = 1;
        /// <summary>DOTween's log behaviour.
        /// <para>Default: LogBehaviour.ErrorsOnly</para></summary>
        public static LogBehaviour logBehaviour {
            get { return _logBehaviour; }
            set { _logBehaviour = value; Debugger.SetLogPriority(_logBehaviour); }
        }
        static LogBehaviour _logBehaviour = LogBehaviour.ErrorsOnly;

        ///////////////////////////////////////////////
        // Default options for Tweens /////////////////

        /// <summary>Default autoPlay behaviour for new tweens.
        /// <para>Default: AutoPlay.All</para></summary>
        public static AutoPlay defaultAutoPlay = AutoPlay.All;
        /// <summary>Default autoKillOnComplete behaviour for new tweens.
        /// <para>Default: TRUE</para></summary>
        public static bool defaultAutoKill = true;
        /// <summary>Default loopType applied to all new tweens.
        /// <para>Default: LoopType.Restart</para></summary>
        public static LoopType defaultLoopType = LoopType.Restart;
        /// <summary>If TRUE all newly created tweens are set as recyclable, otherwise not.
        /// <para>Default: FALSE</para></summary>
        public static bool defaultRecyclable;
        /// <summary>Default ease applied to all new tweens.
        /// <para>Default: Ease.InOutQuad</para></summary>
        public static Ease defaultEaseType = Ease.InOutQuad;
        /// <summary>Default overshoot/amplitude used for eases
        /// <para>Default: 1.70158f</para></summary>
        public static float defaultEaseOvershootOrAmplitude = 1.70158f;
        /// <summary>Default period used for eases
        /// <para>Default: 0</para></summary>
        public static float defaultEasePeriod = 0;

        internal static DOTweenComponent instance; // Assigned/removed by DOTweenComponent.Create/DestroyInstance
        internal static bool isUnityEditor;
        internal static bool isDebugBuild;
        internal static int maxActiveTweenersReached, maxActiveSequencesReached; // Controlled by DOTweenInspector if showUnityEditorReport is active
        internal static readonly List<TweenCallback> GizmosDelegates = new List<TweenCallback>(); // Can be used by other classes to call internal gizmo draw methods
        internal static bool initialized; // Can be set to false by DOTweenComponent OnDestroy

        #region Static Constructor

        static DOTween()
        {
            isUnityEditor = Application.isEditor;
#if DEBUG
            isDebugBuild = true;
#endif
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Must be called once, before the first ever DOTween call/reference,
        /// otherwise it will be called automatically and will use default options.
        /// Calling it a second time won't have any effect.
        /// <para>You can chain <code>SetCapacity</code> to this method, to directly set the max starting size of Tweeners and Sequences:</para>
        /// <code>DOTween.Init(false, false, LogBehaviour.Default).SetCapacity(100, 20);</code>
        /// </summary>
        /// <param name="recycleAllByDefault">If TRUE all new tweens will be set for recycling, meaning that when killed,
        /// instead of being destroyed, they will be put in a pool and reused instead of creating new tweens. This option allows you to avoid
        /// GC allocations by reusing tweens, but you will have to take care of tween references, since they might result active
        /// even if they were killed (since they might have been respawned and are now being used for other tweens).
        /// <para>If you want to automatically set your tween references to NULL when a tween is killed 
        /// you can use the OnKill callback like this:</para>
        /// <code>.OnKill(()=> myTweenReference = null)</code>
        /// <para>You can change this setting at any time by changing the static <see cref="DOTween.defaultRecyclable"/> property,
        /// or you can set the recycling behaviour for each tween separately, using:</para>
        /// <para><code>SetRecyclable(bool recyclable)</code></para>
        /// <para>Default: FALSE</para></param>
        /// <param name="useSafeMode">If TRUE makes tweens slightly slower but safer, automatically taking care of a series of things
        /// (like targets becoming null while a tween is playing).
        /// You can change this setting at any time by changing the static <see cref="DOTween.useSafeMode"/> property.
        /// <para>Default: FALSE</para></param>
        /// <param name="logBehaviour">Type of logging to use.
        /// You can change this setting at any time by changing the static <see cref="DOTween.logBehaviour"/> property.
        /// <para>Default: ErrorsOnly</para></param>
        public static IDOTweenInit Init(bool recycleAllByDefault = false, bool useSafeMode = true, LogBehaviour logBehaviour = LogBehaviour.ErrorsOnly)
        {
            if (initialized) return instance;

            initialized = true;
            // Options
            DOTween.defaultRecyclable = recycleAllByDefault;
            DOTween.useSafeMode = useSafeMode;
            DOTween.logBehaviour = logBehaviour;
            // Gameobject - also assign instance
            DOTweenComponent.Create();
            // Log
            if (Debugger.logPriority >= 2) Debugger.Log("DOTween initialization (useSafeMode: " + useSafeMode + ", logBehaviour: " + logBehaviour + ")");

            return instance;
        }

        /// <summary>
        /// Directly sets the current max capacity of Tweeners and Sequences
        /// (meaning how many Tweeners and Sequences can be running at the same time),
        /// so that DOTween doesn't need to automatically increase them in case the max is reached
        /// (which might lead to hiccups when that happens).
        /// Sequences capacity must be less or equal to Tweeners capacity
        /// (if you pass a low Tweener capacity it will be automatically increased to match the Sequence's).
        /// Beware: use this method only when there are no tweens running.
        /// </summary>
        /// <param name="tweenersCapacity">Max Tweeners capacity.
        /// Default: 200</param>
        /// <param name="sequencesCapacity">Max Sequences capacity.
        /// Default: 50</param>
        public static void SetTweensCapacity(int tweenersCapacity, int sequencesCapacity)
        {
            TweenManager.SetCapacities(tweenersCapacity, sequencesCapacity);
        }

        /// <summary>
        /// Kills all tweens, clears all cached tween pools and plugins and resets the max Tweeners/Sequences capacities to the default values.
        /// </summary>
        /// <param name="destroy">If TRUE also destroys DOTween's gameObject and resets its initializiation, default settings and everything else
        /// (so that next time you use it it will need to be re-initialized)</param>
        public static void Clear(bool destroy = false)
        {
            TweenManager.PurgeAll();
            PluginsManager.PurgeAll();
            if (!destroy) return;

            initialized = false;
            useSafeMode = false;
            showUnityEditorReport = false;
            timeScale = 1;
            logBehaviour = LogBehaviour.ErrorsOnly;
            defaultEaseType = Ease.OutQuad;
            defaultEaseOvershootOrAmplitude = 1.70158f;
            defaultEasePeriod = 0;
            defaultAutoPlay = AutoPlay.All;
            defaultLoopType = LoopType.Restart;
            defaultAutoKill = true;
            defaultRecyclable = false;
            maxActiveTweenersReached = maxActiveSequencesReached = 0;

            DOTweenComponent.DestroyInstance();
        }

        /// <summary>
        /// Clears all cached tween pools.
        /// </summary>
        public static void ClearCachedTweens()
        {
            TweenManager.PurgePools();
        }

        /// <summary>
        /// Checks all active tweens to find and remove eventually invalid ones (usually because their targets became NULL)
        /// and returns the total number of invalid tweens found and removed.
        /// <para>Automatically called when loading a new scene if <see cref="useSafeMode"/> is TRUE.</para>
        /// BEWARE: this is a slightly expensive operation so use it with care
        /// </summary>
        public static int Validate()
        {
            return TweenManager.Validate();
        }

        #endregion

        // ===================================================================================
        // PUBLIC TWEEN CREATION METHODS -----------------------------------------------------

        // Sadly can't make generic versions of default tweens with additional options
        // where the TO method doesn't contain the options param, otherwise the correct Option type won't be inferred.
        // So: overloads. Sigh.
        // Also, Unity has a bug which doesn't allow method overloading with its own implicitly casteable types (like Vector4 and Color)
        // and additional parameters, so in those cases I have to create overloads instead than using optionals. ARARGH!

        #region Tween TO

        /// <summary>Tweens a property or field to the given value using default plugins</summary>
        /// <param name="getter">A getter for the field or property to tween.
        /// <para>Example usage with lambda:</para><code>()=> myProperty</code></param>
        /// <param name="setter">A setter for the field or property to tween
        /// <para>Example usage with lambda:</para><code>x=> myProperty = x</code></param>
        /// <param name="endValue">The end value to reach</param><param name="duration">The tween's duration</param>
        public static TweenerCore<float, float, FloatOptions> To(DOGetter<float> getter, DOSetter<float> setter, float endValue, float duration)
        { return ApplyTo<float, float, FloatOptions>(getter, setter, endValue, duration); }
        /// <summary>Tweens a property or field to the given value using default plugins</summary>
        /// <param name="getter">A getter for the field or property to tween.
        /// <para>Example usage with lambda:</para><code>()=> myProperty</code></param>
        /// <param name="setter">A setter for the field or property to tween
        /// <para>Example usage with lambda:</para><code>x=> myProperty = x</code></param>
        /// <param name="endValue">The end value to reach</param><param name="duration">The tween's duration</param>
        public static Tweener To(DOGetter<int> getter, DOSetter<int> setter, int endValue,float duration)
        { return ApplyTo<int, int, NoOptions>(getter, setter, endValue, duration); }
        /// <summary>Tweens a property or field to the given value using default plugins</summary>
        /// <param name="getter">A getter for the field or property to tween.
        /// <para>Example usage with lambda:</para><code>()=> myProperty</code></param>
        /// <param name="setter">A setter for the field or property to tween
        /// <para>Example usage with lambda:</para><code>x=> myProperty = x</code></param>
        /// <param name="endValue">The end value to reach</param><param name="duration">The tween's duration</param>
        public static Tweener To(DOGetter<uint> getter, DOSetter<uint> setter, uint endValue, float duration)
        { return ApplyTo<uint, uint, NoOptions>(getter, setter, endValue, duration); }
        /// <summary>Tweens a property or field to the given value using default plugins</summary>
        /// <param name="getter">A getter for the field or property to tween.
        /// <para>Example usage with lambda:</para><code>()=> myProperty</code></param>
        /// <param name="setter">A setter for the field or property to tween
        /// <para>Example usage with lambda:</para><code>x=> myProperty = x</code></param>
        /// <param name="endValue">The end value to reach</param><param name="duration">The tween's duration</param>
        public static TweenerCore<string, string, StringOptions> To(DOGetter<string> getter, DOSetter<string> setter, string endValue, float duration)
        { return ApplyTo<string, string, StringOptions>(getter, setter, endValue, duration); }
        /// <summary>Tweens a property or field to the given value using default plugins</summary>
        /// <param name="getter">A getter for the field or property to tween.
        /// <para>Example usage with lambda:</para><code>()=> myProperty</code></param>
        /// <param name="setter">A setter for the field or property to tween
        /// <para>Example usage with lambda:</para><code>x=> myProperty = x</code></param>
        /// <param name="endValue">The end value to reach</param><param name="duration">The tween's duration</param>
        public static TweenerCore<Vector2, Vector2, VectorOptions> To(DOGetter<Vector2> getter, DOSetter<Vector2> setter, Vector2 endValue, float duration)
        { return ApplyTo<Vector2, Vector2, VectorOptions>(getter, setter, endValue, duration); }
        /// <summary>Tweens a property or field to the given value using default plugins</summary>
        /// <param name="getter">A getter for the field or property to tween.
        /// <para>Example usage with lambda:</para><code>()=> myProperty</code></param>
        /// <param name="setter">A setter for the field or property to tween
        /// <para>Example usage with lambda:</para><code>x=> myProperty = x</code></param>
        /// <param name="endValue">The end value to reach</param><param name="duration">The tween's duration</param>
        public static TweenerCore<Vector3, Vector3, VectorOptions> To(DOGetter<Vector3> getter, DOSetter<Vector3> setter, Vector3 endValue, float duration)
        { return ApplyTo<Vector3, Vector3, VectorOptions>(getter, setter, endValue, duration); }
        /// <summary>Tweens a property or field to the given value using default plugins</summary>
        /// <param name="getter">A getter for the field or property to tween.
        /// <para>Example usage with lambda:</para><code>()=> myProperty</code></param>
        /// <param name="setter">A setter for the field or property to tween
        /// <para>Example usage with lambda:</para><code>x=> myProperty = x</code></param>
        /// <param name="endValue">The end value to reach</param><param name="duration">The tween's duration</param>
        public static TweenerCore<Vector4, Vector4, VectorOptions> To(DOGetter<Vector4> getter, DOSetter<Vector4> setter, Vector4 endValue, float duration)
        { return ApplyTo<Vector4, Vector4, VectorOptions>(getter, setter, endValue, duration); }
        /// <summary>Tweens a property or field to the given value using default plugins</summary>
        /// <param name="getter">A getter for the field or property to tween.
        /// <para>Example usage with lambda:</para><code>()=> myProperty</code></param>
        /// <param name="setter">A setter for the field or property to tween
        /// <para>Example usage with lambda:</para><code>x=> myProperty = x</code></param>
        /// <param name="endValue">The end value to reach</param><param name="duration">The tween's duration</param>
        public static TweenerCore<Quaternion, Vector3, QuaternionOptions> To(DOGetter<Quaternion> getter, DOSetter<Quaternion> setter, Vector3 endValue, float duration)
        { return ApplyTo<Quaternion, Vector3, QuaternionOptions>(getter, setter, endValue, duration); }
        /// <summary>Tweens a property or field to the given value using default plugins</summary>
        /// <param name="getter">A getter for the field or property to tween.
        /// <para>Example usage with lambda:</para><code>()=> myProperty</code></param>
        /// <param name="setter">A setter for the field or property to tween
        /// <para>Example usage with lambda:</para><code>x=> myProperty = x</code></param>
        /// <param name="endValue">The end value to reach</param><param name="duration">The tween's duration</param>
        public static TweenerCore<Color, Color, ColorOptions> To(DOGetter<Color> getter, DOSetter<Color> setter, Color endValue, float duration)
        { return ApplyTo<Color, Color, ColorOptions>(getter, setter, endValue, duration); }
        /// <summary>Tweens a property or field to the given value using default plugins</summary>
        /// <param name="getter">A getter for the field or property to tween.
        /// <para>Example usage with lambda:</para><code>()=> myProperty</code></param>
        /// <param name="setter">A setter for the field or property to tween
        /// <para>Example usage with lambda:</para><code>x=> myProperty = x</code></param>
        /// <param name="endValue">The end value to reach</param><param name="duration">The tween's duration</param>
        public static TweenerCore<Rect, Rect, RectOptions> To(DOGetter<Rect> getter, DOSetter<Rect> setter, Rect endValue, float duration)
        { return ApplyTo<Rect, Rect, RectOptions>(getter, setter, endValue, duration); }
        /// <summary>Tweens a property or field to the given value using default plugins</summary>
        /// <param name="getter">A getter for the field or property to tween.
        /// <para>Example usage with lambda:</para><code>()=> myProperty</code></param>
        /// <param name="setter">A setter for the field or property to tween
        /// <para>Example usage with lambda:</para><code>x=> myProperty = x</code></param>
        /// <param name="endValue">The end value to reach</param><param name="duration">The tween's duration</param>
        public static Tweener To(DOGetter<RectOffset> getter, DOSetter<RectOffset> setter, RectOffset endValue, float duration)
        { return ApplyTo<RectOffset, RectOffset, NoOptions>(getter, setter, endValue, duration); }

        /// <summary>Tweens a property or field to the given value using a custom plugin</summary>
        /// <param name="plugin">The plugin to use. Each custom plugin implements a static <code>Get()</code> method
        /// you'll need to call to assign the correct plugin in the correct way, like this:
        /// <para><code>CustomPlugin.Get()</code></para></param>
        /// <param name="getter">A getter for the field or property to tween.
        /// <para>Example usage with lambda:</para><code>()=> myProperty</code></param>
        /// <param name="setter">A setter for the field or property to tween
        /// <para>Example usage with lambda:</para><code>x=> myProperty = x</code></param>
        /// <param name="endValue">The end value to reach</param><param name="duration">The tween's duration</param>
        public static TweenerCore<T1, T2, TPlugOptions> To<T1, T2, TPlugOptions>(
            ABSTweenPlugin<T1, T2, TPlugOptions> plugin, DOGetter<T1> getter, DOSetter<T1> setter, T2 endValue, float duration
        )
            where TPlugOptions : struct
        { return ApplyTo(getter, setter, endValue, duration, plugin); }

        /// <summary>Tweens only one axis of a Vector3 to the given value using default plugins.</summary>
        /// <param name="getter">A getter for the field or property to tween.
        /// <para>Example usage with lambda:</para><code>()=> myProperty</code></param>
        /// <param name="setter">A setter for the field or property to tween
        /// <para>Example usage with lambda:</para><code>x=> myProperty = x</code></param>
        /// <param name="endValue">The end value to reach</param><param name="duration">The tween's duration</param>
        /// <param name="axisConstraint">The axis to tween</param>
        public static TweenerCore<Vector3, Vector3, VectorOptions> ToAxis(DOGetter<Vector3> getter, DOSetter<Vector3> setter, float endValue, float duration, AxisConstraint axisConstraint = AxisConstraint.X)
        {
            TweenerCore<Vector3, Vector3, VectorOptions> t = ApplyTo<Vector3, Vector3, VectorOptions>(getter, setter, new Vector3(endValue, endValue, endValue), duration);
            t.plugOptions.axisConstraint = axisConstraint;
            return t;
        }
        /// <summary>Tweens only the alpha of a Color to the given value using default plugins</summary>
        /// <param name="getter">A getter for the field or property to tween.
        /// <para>Example usage with lambda:</para><code>()=> myProperty</code></param>
        /// <param name="setter">A setter for the field or property to tween
        /// <para>Example usage with lambda:</para><code>x=> myProperty = x</code></param>
        /// <param name="endValue">The end value to reach</param><param name="duration">The tween's duration</param>
        public static Tweener ToAlpha(DOGetter<Color> getter, DOSetter<Color> setter, float endValue, float duration)
        { return ApplyTo<Color, Color, ColorOptions>(getter, setter, new Color(0, 0, 0, endValue), duration).SetOptions(true); }

        #endregion

        #region Special TOs (No FROMs)

        /// <summary>Tweens a virtual property from the given start to the given end value 
        /// and implements a setter that allows to use that value with an external method or a lambda
        /// <para>Example:</para>
        /// <code>To(MyMethod, 0, 12, 0.5f);</code>
        /// <para>Where MyMethod is a function that accepts a float parameter (which will be the result of the virtual tween)</para></summary>
        /// <param name="setter">The action to perform with the tweened value</param>
        /// <param name="startValue">The value to start from</param>
        /// <param name="endValue">The end value to reach</param>
        /// <param name="duration">The duration of the virtual tween
        /// </param>
        public static Tweener To(DOSetter<float> setter, float startValue, float endValue, float duration)
        {
            float v = startValue;
            return To(() => v, x => { v = x; setter(v); }, endValue, duration)
                .NoFrom();
        }

        /// <summary>Punches a Vector3 towards the given direction and then back to the starting one
        /// as if it was connected to the starting position via an elastic.
        /// <para>This tween type generates some GC allocations at startup</para></summary>
        /// <param name="getter">A getter for the field or property to tween.
        /// <para>Example usage with lambda:</para><code>()=> myProperty</code></param>
        /// <param name="setter">A setter for the field or property to tween
        /// <para>Example usage with lambda:</para><code>x=> myProperty = x</code></param>
        /// <param name="direction">The direction and strength of the punch</param>
        /// <param name="duration">The duration of the tween</param>
        /// <param name="vibrato">Indicates how much will the punch vibrate</param>
        /// <param name="elasticity">Represents how much (0 to 1) the vector will go beyond the starting position when bouncing backwards.
        /// 1 creates a full oscillation between the direction and the opposite decaying direction,
        /// while 0 oscillates only between the starting position and the decaying direction</param>
        public static TweenerCore<Vector3, Vector3[], Vector3ArrayOptions> Punch(DOGetter<Vector3> getter, DOSetter<Vector3> setter, Vector3 direction, float duration, int vibrato = 10, float elasticity = 1)
        {
            if (elasticity > 1) elasticity = 1;
            else if (elasticity < 0) elasticity = 0;
            float strength = direction.magnitude;
            int totIterations = (int)(vibrato * duration);
            if (totIterations < 2) totIterations = 2;
            float decayXTween = strength / totIterations;
            // Calculate and store the duration of each tween
            float[] tDurations = new float[totIterations];
            float sum = 0;
            for (int i = 0; i < totIterations; ++i) {
                float iterationPerc = (i + 1) / (float)totIterations;
                float tDuration = duration * iterationPerc;
                sum += tDuration;
                tDurations[i] = tDuration;
            }
            float tDurationMultiplier = duration / sum; // Multiplier that allows the sum of tDurations to equal the set duration
            for (int i = 0; i < totIterations; ++i) tDurations[i] = tDurations[i] * tDurationMultiplier;
            // Create the tween
            Vector3[] tos = new Vector3[totIterations];
            for (int i = 0; i < totIterations; ++i) {
                if (i < totIterations - 1) {
                    if (i == 0) tos[i] = direction;
                    else if (i % 2 != 0) tos[i] = -Vector3.ClampMagnitude(direction, strength * elasticity);
                    else tos[i] = Vector3.ClampMagnitude(direction, strength);
                    strength -= decayXTween;
                } else tos[i] = Vector3.zero;
            }
            return ToArray(getter, setter, tos, tDurations)
                .NoFrom()
                .SetSpecialStartupMode(SpecialStartupMode.SetPunch);
        }

        /// <summary>Shakes a Vector3 with the given values.</summary>
        /// <param name="getter">A getter for the field or property to tween.
        /// <para>Example usage with lambda:</para><code>()=> myProperty</code></param>
        /// <param name="setter">A setter for the field or property to tween
        /// <para>Example usage with lambda:</para><code>x=> myProperty = x</code></param>
        /// <param name="duration">The duration of the tween</param>
        /// <param name="strength">The shake strength</param>
        /// <param name="vibrato">Indicates how much will the shake vibrate</param>
        /// <param name="randomness">Indicates how much the shake will be random (0 to 180 - values higher than 90 kind of suck, so beware). 
        /// Setting it to 0 will shake along a single direction and behave like a random punch.</param>
        /// <param name="ignoreZAxis">If TRUE only shakes on the X Y axis (looks better with things like cameras).</param>
        public static TweenerCore<Vector3, Vector3[], Vector3ArrayOptions> Shake(DOGetter<Vector3> getter, DOSetter<Vector3> setter, float duration, float strength = 3, int vibrato = 10, float randomness = 90, bool ignoreZAxis = true)
        {
            int totIterations = (int)(vibrato * duration);
            if (totIterations < 2) totIterations = 2;
            float decayXTween = strength / totIterations;
            // Calculate and store the duration of each tween
            float[] tDurations = new float[totIterations];
            float sum = 0;
            for (int i = 0; i < totIterations; ++i) {
                float iterationPerc = (i + 1) / (float)totIterations;
                float tDuration = duration * iterationPerc;
                sum += tDuration;
                tDurations[i] = tDuration;
            }
            float tDurationMultiplier = duration / sum; // Multiplier that allows the sum of tDurations to equal the set duration
            for (int i = 0; i < totIterations; ++i) tDurations[i] = tDurations[i] * tDurationMultiplier;
            // Create the tween
            float ang = UnityEngine.Random.Range(0f, 360f);
            Vector3[] tos = new Vector3[totIterations];
            for (int i = 0; i < totIterations; ++i) {
                if (i < totIterations - 1) {
                    if (i > 0) ang = ang - 180 + UnityEngine.Random.Range(-randomness, randomness);
                    if (ignoreZAxis) {
                        tos[i] = Utils.Vector3FromAngle(ang, strength);
                    } else {
                        Quaternion rndQuaternion = Quaternion.AngleAxis(UnityEngine.Random.Range(-randomness, randomness), Vector3.up);
                        tos[i] = rndQuaternion * Utils.Vector3FromAngle(ang, strength);
                    }
                    strength -= decayXTween;
                } else tos[i] = Vector3.zero;
            }
            return ToArray(getter, setter, tos, tDurations)
                .NoFrom().SetSpecialStartupMode(SpecialStartupMode.SetShake);
        }

        /// <summary>Tweens a property or field to the given values using default plugins.
        /// Ease is applied between each segment and not as a whole.
        /// <para>This tween type generates some GC allocations at startup</para></summary>
        /// <param name="getter">A getter for the field or property to tween.
        /// <para>Example usage with lambda:</para><code>()=> myProperty</code></param>
        /// <param name="setter">A setter for the field or property to tween
        /// <para>Example usage with lambda:</para><code>x=> myProperty = x</code></param>
        /// <param name="endValues">The end values to reach for each segment. This array must have the same length as <code>durations</code></param>
        /// <param name="durations">The duration of each segment. This array must have the same length as <code>endValues</code></param>
        public static TweenerCore<Vector3, Vector3[], Vector3ArrayOptions> ToArray(DOGetter<Vector3> getter, DOSetter<Vector3> setter, Vector3[] endValues, float[] durations)
        {
            int len = durations.Length;
            if (len != endValues.Length) {
                Debugger.LogError("To Vector3 array tween: endValues and durations arrays must have the same length");
                return null;
            }

            // Clone the arrays
            Vector3[] endValuesClone = new Vector3[len];
            float[] durationsClone = new float[len];
            for (int i = 0; i < len; i++) {
                endValuesClone[i] = endValues[i];
                durationsClone[i] = durations[i];
            }

            float totDuration = 0;
            for (int i = 0; i < len; ++i) totDuration += durationsClone[i];
            TweenerCore<Vector3, Vector3[], Vector3ArrayOptions> t =
                ApplyTo<Vector3, Vector3[], Vector3ArrayOptions>(getter, setter, endValuesClone, totDuration)
                    .NoFrom();
            t.plugOptions.durations = durationsClone;
            return t;
        }

        #endregion

        #region Tween SEQUENCE

        /// <summary>
        /// Returns a new <see cref="Sequence"/> to be used for tween groups
        /// </summary>
        public static Sequence Sequence()
        {
            InitCheck();
            Sequence sequence = TweenManager.GetSequence();
            Tweening.Sequence.Setup(sequence);
            return sequence;
        }
        #endregion

        /////////////////////////////////////////////////////////////////////
        // OTHER STUFF //////////////////////////////////////////////////////

        #region Play Operations

        /// <summary>Completes all tweens and returns the number of actual tweens completed
        /// (meaning tweens that don't have infinite loops and were not already complete)</summary>
        public static int Complete()
        {
            return TweenManager.FilteredOperation(OperationType.Complete, FilterType.All, null, false, 0);
        }
        /// <summary>Completes all tweens with the given ID or target and returns the number of actual tweens completed
        /// (meaning the tweens that don't have infinite loops and were not already complete)</summary>
        public static int Complete(object targetOrId)
        {
            if (targetOrId == null) return 0;
            return TweenManager.FilteredOperation(OperationType.Complete, FilterType.TargetOrId, targetOrId, false, 0);
        }

        /// <summary>Flips all tweens (changing their direction to forward if it was backwards and viceversa),
        /// then returns the number of actual tweens flipped</summary>
        public static int Flip()
        {
            return TweenManager.FilteredOperation(OperationType.Flip, FilterType.All, null, false, 0);
        }
        /// <summary>Flips the tweens with the given ID or target (changing their direction to forward if it was backwards and viceversa),
        /// then returns the number of actual tweens flipped</summary>
        public static int Flip(object targetOrId)
        {
            if (targetOrId == null) return 0;
            return TweenManager.FilteredOperation(OperationType.Flip, FilterType.TargetOrId, targetOrId, false, 0);
        }

        /// <summary>Sends all tweens to the given position (calculating also eventual loop cycles) and returns the actual tweens involved</summary>
        public static int Goto(float to, bool andPlay = false)
        {
            return TweenManager.FilteredOperation(OperationType.Goto, FilterType.All, null, andPlay, to);
        }
        /// <summary>Sends all tweens with the given ID or target to the given position (calculating also eventual loop cycles)
        /// and returns the actual tweens involved</summary>
        public static int Goto(object targetOrId, float to, bool andPlay = false)
        {
            if (targetOrId == null) return 0;
            return TweenManager.FilteredOperation(OperationType.Goto, FilterType.TargetOrId, targetOrId, andPlay, to);
        }

        /// <summary>Kills all tweens and returns the number of actual tweens killed</summary>
        public static int Kill()
        {
            return TweenManager.DespawnAll();
        }
        /// <summary>Kills all tweens with the given ID or target and returns the number of actual tweens killed</summary>
        public static int Kill(object targetOrId)
        {
            if (targetOrId == null) return 0;
            return TweenManager.FilteredOperation(OperationType.Despawn, FilterType.TargetOrId, targetOrId, false, 0);
        }

        /// <summary>Pauses all tweens and returns the number of actual tweens paused</summary>
        public static int Pause()
        {
            return TweenManager.FilteredOperation(OperationType.Pause, FilterType.All, null, false, 0);
        }
        /// <summary>Pauses all tweens with the given ID or target and returns the number of actual tweens paused
        /// (meaning the tweens that were actually playing and have been paused)</summary>
        public static int Pause(object targetOrId)
        {
            if (targetOrId == null) return 0;
            return TweenManager.FilteredOperation(OperationType.Pause, FilterType.TargetOrId, targetOrId, false, 0);
        }

        /// <summary>Plays all tweens and returns the number of actual tweens played
        /// (meaning tweens that were not already playing or complete)</summary>
        public static int Play()
        {
            return TweenManager.FilteredOperation(OperationType.Play, FilterType.All, null, false, 0);
        }
        /// <summary>Plays all tweens with the given ID or target and returns the number of actual tweens played
        /// (meaning the tweens that were not already playing or complete)</summary>
        public static int Play(object targetOrId)
        {
            if (targetOrId == null) return 0;
            return TweenManager.FilteredOperation(OperationType.Play, FilterType.TargetOrId, targetOrId, false, 0);
        }

        /// <summary>Plays backwards all tweens and returns the number of actual tweens played
        /// (meaning tweens that were not already started, playing backwards or rewinded)</summary>
        public static int PlayBackwards()
        {
            return TweenManager.FilteredOperation(OperationType.PlayBackwards, FilterType.All, null, false, 0);
        }
        /// <summary>Plays backwards all tweens with the given ID or target and returns the number of actual tweens played
        /// (meaning the tweens that were not already started, playing backwards or rewinded)</summary>
        public static int PlayBackwards(object targetOrId)
        {
            if (targetOrId == null) return 0;
            return TweenManager.FilteredOperation(OperationType.PlayBackwards, FilterType.TargetOrId, targetOrId, false, 0);
        }

        /// <summary>Plays forward all tweens and returns the number of actual tweens played
        /// (meaning tweens that were not already playing forward or complete)</summary>
        public static int PlayForward()
        {
            return TweenManager.FilteredOperation(OperationType.PlayForward, FilterType.All, null, false, 0);
        }
        /// <summary>Plays forward all tweens with the given ID or target and returns the number of actual tweens played
        /// (meaning the tweens that were not already playing forward or complete)</summary>
        public static int PlayForward(object targetOrId)
        {
            if (targetOrId == null) return 0;
            return TweenManager.FilteredOperation(OperationType.PlayForward, FilterType.TargetOrId, targetOrId, false, 0);
        }

        /// <summary>Restarts all tweens, then returns the number of actual tweens restarted</summary>
        public static int Restart(bool includeDelay = true)
        {
            return TweenManager.FilteredOperation(OperationType.Restart, FilterType.All, null, includeDelay, 0);
        }
        /// <summary>Restarts all tweens with the given ID or target, then returns the number of actual tweens restarted</summary>
        public static int Restart(object targetOrId, bool includeDelay = true)
        {
            if (targetOrId == null) return 0;
            return TweenManager.FilteredOperation(OperationType.Restart, FilterType.TargetOrId, targetOrId, includeDelay, 0);
        }

        /// <summary>Rewinds and pauses all tweens, then returns the number of actual tweens rewinded
        /// (meaning tweens that were not already rewinded)</summary>
        public static int Rewind(bool includeDelay = true)
        {
            return TweenManager.FilteredOperation(OperationType.Rewind, FilterType.All, null, includeDelay, 0);
        }
        /// <summary>Rewinds and pauses all tweens with the given ID or target, then returns the number of actual tweens rewinded
        /// (meaning the tweens that were not already rewinded)</summary>
        public static int Rewind(object targetOrId, bool includeDelay = true)
        {
            if (targetOrId == null) return 0;
            return TweenManager.FilteredOperation(OperationType.Rewind, FilterType.TargetOrId, targetOrId, includeDelay, 0);
        }

        /// <summary>Toggles the play state of all tweens and returns the number of actual tweens toggled
        /// (meaning tweens that could be played or paused, depending on the toggle state)</summary>
        public static int TogglePause()
        {
            return TweenManager.FilteredOperation(OperationType.TogglePause, FilterType.All, null, false, 0);
        }
        /// <summary>Toggles the play state of all tweens with the given ID or target and returns the number of actual tweens toggled
        /// (meaning the tweens that could be played or paused, depending on the toggle state)</summary>
        public static int TogglePause(object targetOrId)
        {
            if (targetOrId == null) return 0;
            return TweenManager.FilteredOperation(OperationType.TogglePause, FilterType.TargetOrId, targetOrId, false, 0);
        }
        #endregion

        #region Global Info Getters

        /// <summary>
        /// Returns TRUE if a tween with the given ID or target is active (regardless if it's playing or not).
        /// <para>You can also use this to know if a shortcut tween is active for a given target.</para>
        /// <para>Example:</para>
        /// <para><code>transform.DOMoveX(45, 1); // transform is automatically added as the tween target</code></para>
        /// <para><code>DOTween.IsTweening(transform); // Returns true</code></para>
        /// </summary>
        public static bool IsTweening(object targetOrId)
        {
            return TweenManager.FilteredOperation(OperationType.IsTweening, FilterType.TargetOrId, targetOrId, false, 0) > 0;
        }

        /// <summary>
        /// Returns the total number of active and playing tweens.
        /// A tween is considered as playing even if its delay is actually playing
        /// </summary>
        public static int TotPlayingTweens()
        {
            return TweenManager.TotPlayingTweens();
        }

        #endregion

        // ===================================================================================
        // METHODS ---------------------------------------------------------------------------

        static void InitCheck()
        {
            if (initialized) return;

            Init(defaultRecyclable, useSafeMode, logBehaviour);
            Debugger.LogWarning("DOTween auto-initialized with default settings (recycleAllByDefault: " + defaultRecyclable + ", useSafeMode: " + useSafeMode + ", logBehaviour: " + logBehaviour + "). Call DOTween.Init before creating your first tween in order to choose the settings yourself");
        }

        static TweenerCore<T1, T2, TPlugOptions> ApplyTo<T1, T2, TPlugOptions>(
            DOGetter<T1> getter, DOSetter<T1> setter, T2 endValue, float duration, ABSTweenPlugin<T1, T2, TPlugOptions> plugin = null
        )
            where TPlugOptions : struct
        {
            InitCheck();
            TweenerCore<T1, T2, TPlugOptions> tweener = TweenManager.GetTweener<T1, T2, TPlugOptions>();
            if (!Tweener.Setup(tweener, getter, setter, endValue, duration, plugin)) {
                TweenManager.Despawn(tweener);
                return null;
            }
            return tweener;
        }
    }
}