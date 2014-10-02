﻿// Author: Daniele Giardini - http://www.demigiant.com
// Created: 2014/07/10 19:17
// 
// License Copyright (c) Daniele Giardini.
// This work is subject to the terms at http://dotween.demigiant.com/license.php

using System;
using DG.Tweening.Core;
using DG.Tweening.Core.Easing;
using DG.Tweening.Plugins.Core;
using DG.Tweening.Plugins.Options;
using UnityEngine;

#pragma warning disable 1591
namespace DG.Tweening.Plugins
{
    public class RectPlugin : ABSTweenPlugin<Rect, Rect, RectOptions>
    {
        public override void Reset(TweenerCore<Rect, Rect, RectOptions> t) { }

        public override void SetFrom(TweenerCore<Rect, Rect, RectOptions> t, bool isRelative)
        {
            Rect prevEndVal = t.endValue;
            t.endValue = t.getter();
            t.startValue = prevEndVal;
            if (isRelative) {
                t.startValue.x += t.endValue.x;
                t.startValue.y += t.endValue.y;
                t.startValue.width += t.endValue.width;
                t.startValue.height += t.endValue.height;
            }
            Rect to = t.startValue;
            if (t.plugOptions.snapping) {
                to.x = (float)Math.Round(to.x);
                to.y = (float)Math.Round(to.y);
                to.width = (float)Math.Round(to.width);
                to.height = (float)Math.Round(to.height);
            }
            t.setter(to);
        }

        public override Rect ConvertToStartValue(TweenerCore<Rect, Rect, RectOptions> t, Rect value)
        {
            return value;
        }

        public override void SetRelativeEndValue(TweenerCore<Rect, Rect, RectOptions> t)
        {
            t.endValue.x += t.startValue.x;
            t.endValue.y += t.startValue.y;
            t.endValue.width += t.startValue.width;
            t.endValue.height += t.startValue.height;
        }

        public override void SetChangeValue(TweenerCore<Rect, Rect, RectOptions> t)
        {
            t.changeValue = new Rect(
                t.endValue.x - t.startValue.x,
                t.endValue.y - t.startValue.y,
                t.endValue.width - t.startValue.width,
                t.endValue.height - t.startValue.height
            );
        }

        public override float GetSpeedBasedDuration(RectOptions options, float unitsXSecond, Rect changeValue)
        {
            // Uses length of diagonal to calculate units.
            float diffW = changeValue.width;
            float diffH = changeValue.height;
            float diag = (float)Math.Sqrt(diffW * diffW + diffH * diffH);
            return diag / unitsXSecond;
        }

        public override void EvaluateAndApply(RectOptions options, Tween t, bool isRelative, DOGetter<Rect> getter, DOSetter<Rect> setter, float elapsed, Rect startValue, Rect changeValue, float duration)
        {
            if (t.loopType == LoopType.Incremental) {
                int iterations = t.isComplete ? t.completedLoops - 1 : t.completedLoops;
                startValue.x += changeValue.x * iterations;
                startValue.y += changeValue.y * iterations;
                startValue.width += changeValue.width * iterations;
                startValue.height += changeValue.height * iterations;
            }

            startValue.x = EaseManager.Evaluate(t, elapsed, startValue.x, changeValue.x, duration, t.easeOvershootOrAmplitude, t.easePeriod);
            startValue.y = EaseManager.Evaluate(t, elapsed, startValue.y, changeValue.y, duration, t.easeOvershootOrAmplitude, t.easePeriod);
            startValue.width = EaseManager.Evaluate(t, elapsed, startValue.width, changeValue.width, duration, t.easeOvershootOrAmplitude, t.easePeriod);
            startValue.height = EaseManager.Evaluate(t, elapsed, startValue.height, changeValue.height, duration, t.easeOvershootOrAmplitude, t.easePeriod);
            if (options.snapping) {
                startValue.x = (float)Math.Round(startValue.x);
                startValue.y = (float)Math.Round(startValue.y);
                startValue.width = (float)Math.Round(startValue.width);
                startValue.height = (float)Math.Round(startValue.height);
            }
            setter(startValue);
        }
    }
}