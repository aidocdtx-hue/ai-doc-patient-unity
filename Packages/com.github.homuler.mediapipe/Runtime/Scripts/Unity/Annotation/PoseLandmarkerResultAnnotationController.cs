// Copyright (c) 2023 homuler
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System.Collections.Generic;
using Google.Protobuf.WellKnownTypes;
using Mediapipe.Tasks.Components.Containers;
using Mediapipe.Tasks.Vision.PoseLandmarker;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

namespace Mediapipe.Unity
{
  public class PoseLandmarkerResultAnnotationController : AnnotationController<MultiPoseLandmarkListWithMaskAnnotation>
  {
    [SerializeField] private bool _visualizeZ = false;

    private readonly object _currentTargetLock = new object();
    private PoseLandmarkerResult _currentTarget;

    public void InitScreen(int maskWidth, int maskHeight) => annotation.InitMask(maskWidth, maskHeight);

    public void DrawNow(PoseLandmarkerResult target)
    {
      target.CloneTo(ref _currentTarget);
      if (_currentTarget.segmentationMasks != null)
      {
        ReadMask(_currentTarget.segmentationMasks);
        // NOTE: segmentationMasks can still be accessed from newTarget.
        _currentTarget.segmentationMasks.Clear();
      }
      SyncNow();
    }

    public void DrawLater(PoseLandmarkerResult target) => UpdateCurrentTarget(target);

    private void ReadMask(IReadOnlyList<Image> segmentationMasks) => annotation.ReadMask(segmentationMasks, isMirrored);

    protected void UpdateCurrentTarget(PoseLandmarkerResult newTarget)
    {
      lock (_currentTargetLock)
      {
        newTarget.CloneTo(ref _currentTarget);
        if (_currentTarget.segmentationMasks != null)
        {
          ReadMask(_currentTarget.segmentationMasks);
          // NOTE: segmentationMasks can still be accessed from newTarget.
          _currentTarget.segmentationMasks.Clear();
        }
        isStale = true;
      }
    }

    protected override void SyncNow()
    {
      lock (_currentTargetLock)
      {
        isStale = false;
        annotation.Draw(_currentTarget.poseLandmarks, _visualizeZ);
                
            }
    }

        public Mediapipe.Tasks.Components.Containers.NormalizedLandmark GetLandmark(int i)
        {
            lock (_currentTargetLock)
            {
                if (_currentTarget.poseLandmarks == null || _currentTarget.poseLandmarks[0].landmarks.Count <= i) //Equals(null)은 안됨
                {
                    Mediapipe.Tasks.Components.Containers.NormalizedLandmark dummy = new Tasks.Components.Containers.NormalizedLandmark(-999f, -999f, -999f, 0, 0);
                    return dummy;
                }
                else
                {
                    return _currentTarget.poseLandmarks[0].landmarks[i]; //스레드가 여기로 접근할 때 간헐적으로 IndexOutOfRange 발생
                }
            }
        }
    }
}
