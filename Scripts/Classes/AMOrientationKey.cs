using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using Holoville.HOTween;
using Holoville.HOTween.Core;
using Holoville.HOTween.Plugins.Core;

public class AMPlugOrientation : ABSTweenPlugin {
    internal static System.Type[] validPropTypes = { typeof(Quaternion) };
    internal static System.Type[] validValueTypes = { typeof(Quaternion) };

    Transform sTarget;
    Transform eTarget;

    Quaternion changeVal;

    protected override object startVal { get { return _startVal; } set { _startVal = value; } }

    protected override object endVal { get { return _endVal; } set { _endVal = value; } }

    public AMPlugOrientation(Transform start, Transform end)
        : base(null, false) { this.sTarget = start; this.eTarget = end; }

    public AMPlugOrientation(Transform start, Transform end, bool isRelative)
        : base(null, isRelative) { this.sTarget = start; this.eTarget = end; }

    public AMPlugOrientation(Transform start, Transform end, EaseType easeType)
        : base(null, easeType, false) { this.sTarget = start; this.eTarget = end; }

    public AMPlugOrientation(Transform start, Transform end, EaseType easeType, bool isRelative)
        : base(null, easeType, isRelative) { this.sTarget = start; this.eTarget = end; }

    public AMPlugOrientation(Transform start, Transform end, AnimationCurve curve)
        : base(null, curve, false) { this.sTarget = start; this.eTarget = end; }

    public AMPlugOrientation(Transform start, Transform end, AnimationCurve curve, bool isRelative)
        : base(null, curve, isRelative) { this.sTarget = start; this.eTarget = end; }

    protected override float GetSpeedBasedDuration(float p_speed) {
        return p_speed;
    }

    protected override void SetChangeVal() {
    }

    protected override void SetIncremental(int p_diffIncr) {
    }
    protected override void SetIncrementalRestart() { }

    protected override void DoUpdate(float p_totElapsed) {
        Transform t = tweenObj.target as Transform;

        if(sTarget == null && eTarget == null)
            return;
        else if(sTarget == null)
            t.LookAt(eTarget);
        else if(eTarget == null || sTarget == eTarget)
            t.LookAt(sTarget);
        else {
            float time = ease(p_totElapsed, 0f, 1f, _duration, tweenObj.easeOvershootOrAmplitude, tweenObj.easePeriod);

            Quaternion s = Quaternion.LookRotation(sTarget.position - t.position);
            Quaternion e = Quaternion.LookRotation(eTarget.position - t.position);

            t.rotation = Quaternion.Slerp(s, e, time);
        }
    }

    protected override void SetValue(object p_value) { }
    protected override object GetValue() { return null; }
}

[AddComponentMenu("")]
public class AMOrientationKey : AMKey {

	[SerializeField]
    Transform target;
	[SerializeField]
	string targetPath;

    public int endFrame;

	public void SetTarget(AMITarget itarget, Transform t) {
		if(itarget.isMeta) {
            target = null;
            targetPath = AMUtil.GetPath(itarget.root, t);
			itarget.SetCache(targetPath, t);
		}
		else {
			target = t;
			targetPath = "";
		}
	}
	public Transform GetTarget(AMITarget itarget) {
		Transform ret = null;
		if(itarget.isMeta) {
			if(!string.IsNullOrEmpty(targetPath)) {
				ret = itarget.GetCache(targetPath);
				if(ret == null) {
					ret = AMUtil.GetTarget(itarget.root, targetPath);
                    itarget.SetCache(targetPath, ret);
				}
			}
		}
		else
			ret = target;
		return ret;
	}

	public override void maintainKey(AMITarget itarget, UnityEngine.Object targetObj) {
		if(itarget.isMeta) {
			if(string.IsNullOrEmpty(targetPath)) {
				if(target) {
					targetPath = AMUtil.GetPath(itarget.root, target);
					itarget.SetCache(targetPath, target);
				}
			}

			target = null;
		}
		else {
			if(!target) {
				if(!string.IsNullOrEmpty(targetPath)) {
					target = itarget.GetCache(targetPath);
					if(!target)
						target = AMUtil.GetTarget(itarget.root, targetPath);
				}
			}

			targetPath = "";
		}
	}

    public override void CopyTo(AMKey key) {
		AMOrientationKey a = key as AMOrientationKey;
        a.enabled = false;
        a.frame = frame;
        a.target = target;
		a.targetPath = targetPath;
        a.easeType = easeType;
        a.customEase = new List<float>(customEase);
    }

	public override int getNumberOfFrames(int frameRate) {
        if(!canTween && (endFrame == -1 || endFrame == frame))
            return 1;
        else if(endFrame == -1)
            return -1;
        return endFrame - frame;
	}
	
	public Quaternion getQuaternionAtPercent(Transform obj, Transform tgt, Transform tgte, float percentage) {
        if(tgt == tgte || !canTween) {
			return Quaternion.LookRotation(tgt.position - obj.position);
		}
		
		Quaternion s = Quaternion.LookRotation(tgt.position - obj.position);
		Quaternion e = Quaternion.LookRotation(tgte.position - obj.position);
		
		float time = 0.0f;
		
		if(hasCustomEase()) {
			time = AMUtil.EaseCustom(0.0f, 1.0f, percentage, easeCurve);
		}
		else {
			TweenDelegate.EaseFunc ease = AMUtil.GetEasingFunction((EaseType)easeType);
			time = ease(percentage, 0.0f, 1.0f, 1.0f, amplitude, period);
		}
		
		return Quaternion.Slerp(s, e, time);
	}

    #region action
    public override void build(AMSequence seq, AMTrack track, int index, UnityEngine.Object obj) {
        if(!obj) return;
        int frameRate = seq.take.frameRate;
        if(!canTween) {
            seq.Insert(this, HOTween.To(obj, getTime(frameRate), new TweenParms().Prop("rotation", new AMPlugOrientation(GetTarget(seq.target), null))));
		}
        if(endFrame == -1) return;
        Transform tgt = GetTarget(seq.target), tgte = (track.keys[index+1] as AMOrientationKey).GetTarget(seq.target);
		if(tgt == tgte) {
			seq.Insert(this, HOTween.To(obj, getTime(frameRate), new TweenParms().Prop("rotation", new AMPlugOrientation(tgt, null))));
        }
        else {
            if(hasCustomEase()) {
				seq.Insert(this, HOTween.To(obj, getTime(frameRate), new TweenParms().Prop("rotation", new AMPlugOrientation(tgt, tgte)).Ease(easeCurve)));
            }
            else {
				seq.Insert(this, HOTween.To(obj, getTime(frameRate), new TweenParms().Prop("rotation", new AMPlugOrientation(tgt, tgte)).Ease((EaseType)easeType, amplitude, period)));
            }
        }
    }
    #endregion
}
