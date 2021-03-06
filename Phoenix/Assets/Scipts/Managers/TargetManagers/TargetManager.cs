﻿using UnityEngine;
using System.Collections.Generic;

namespace Phoenix
{

    public class TargetManager : MonoBehaviour
    {
        public bool inWave = false;

        #region Wave Generation Settings
        private float PLAYAREAOFFSET = Mathf.Deg2Rad * 90;
        //Wave Constants
        private float PLAYAREA=Mathf.Deg2Rad*90;
        private float ANCHORLOGBASE = 20;
        private float DISTANCESCALECONSTANT = 2;
        private float TIMESCALE = 10;
        //Anchor Constants
        private float TARGETLOGBASE = 2;
        private float ANCHORDISTANCE = 5;
        private float ANCHORBASEDISTANCE = 10;
        private float ANCHORRADIUSBASE = 5;
        private float ANCHORRADIUSVARIANCE = 2;

        //Target Constants
        private float DISTANCELOGBASE = 2;
        private float MAXTARGETSCALE = 0.03f;
        private float MINTARGETSCALE = 0.02F;
        private float TARGETSPEEDSCALE = 0.1F;

        #endregion

        private float timeCounter;

        private AnchorInfo[] _anchorList;
        private int _anchorCount;
        private int _anchorOn;
        private GameObject[] _activeAnchor;

        [SerializeField]
        private ObjectPool _getAnchor;
        private IEnumerator<AnchorInfo> _anchorIenumerator;
        private GameObject anchorPrefab;
        

        private IEnumerator<AnchorInfo> anchorList()
        {
            for(int i=0;i<_anchorList.Length;i++)
                yield return _anchorList[i];
        }
        

        #region Initialize
        /// <summary>
        /// Initializes the list of anchors so that it can spawn them during the wave
        /// </summary>
        /// <param name="difficultyRating">The difficulty of the wave based on how well the player did the last wave</param>
        /// <param name="difficultySetting">The setting the player chose for difficulty.  Easy =1, Medium = 2, Hard = 3</param>
        public void initializeWave(float difficultyRating,int difficultySetting)
        {
            int anchorCount = Mathf.RoundToInt(Mathf.Log(difficultyRating, ANCHORLOGBASE) - 0.5f);
            _anchorCount = anchorCount;
            _anchorList = new AnchorInfo[anchorCount];
            for (int i = 0; i < anchorCount; i++)
            {
                AnchorInfo currentAnchor;
                //gives anchor a list of targets to spawn
                currentAnchor.targetList=(generateTargetList(difficultyRating / anchorCount,difficultySetting));
                //Generate Anchor position in polar coordinates, with 0 radians being directly forward, around the player
                float angle = (Random.value-0.5F) * PLAYAREA+PLAYAREAOFFSET;
                float distance = (anchorCount + Random.value - 0.5f) * ANCHORDISTANCE + ANCHORBASEDISTANCE;
                float anchorRadius = (Random.value - 0.5F) * ANCHORRADIUSVARIANCE + ANCHORRADIUSBASE;
                //convert polar coordinates to cartesian
                Vector3 position = new Vector3(distance * Mathf.Cos(angle), 1, distance * Mathf.Sin(angle));
                //Randomize initial Movement Vector
                Vector3 initialMovement = new Vector3(Random.value, Random.value, Random.value).normalized;
                currentAnchor.initialPosition = position;
                currentAnchor.initialMovement = initialMovement;
                currentAnchor.moveSet = MovementManager.generateAnchorMovement(initialMovement, 
                        anchorRadius, 
                        difficultySetting, 
                        difficultySetting / 6f);
                _anchorList[i] = currentAnchor;
            }
            //Resets the enumerator to start
            _anchorOn = -1;
        }
        /// <summary>
        /// Generate list of Targets based on difficulty level
        /// </summary>
        /// <param name="difficulty"></param>
        /// <param name="difficultySetting"></param>
        /// <returns></returns>
        private TargetInfo[] generateTargetList(float difficulty,int difficultySetting)
        {
            int targetCount = Mathf.RoundToInt(Mathf.Log(difficulty, TARGETLOGBASE) - 0.5f);
            TargetInfo[] tList = new TargetInfo[targetCount];
            for (int i = 0; i < targetCount; i++) 
            {
                tList[i] = generateTarget(difficulty / targetCount,difficultySetting);
            }
            return tList;
        }

        /// <summary>
        /// Generate Target based on Difficulty rating
        /// </summary>
        /// <param name="difficulty"></param>
        /// <param name="difficultySetting"></param>
        /// <returns></returns>
        private TargetInfo generateTarget(float difficulty,int difficultySetting)
        {
            TargetInfo gen;
            float maxDistance = Mathf.Log(difficulty, DISTANCELOGBASE) * MAXTARGETSCALE;
            float minDistance = Mathf.Log(difficulty, DISTANCELOGBASE * 2)*MINTARGETSCALE;
            //randomize Position and initial movement vector
            Vector3 position = Random.insideUnitSphere*maxDistance;
            Vector3 movement = Random.onUnitSphere;
            movement = movement.normalized;
            gen.initialPosition = position;
            gen.initialMovement = movement;
            gen.moveSet =  MovementManager.generateTargetMovement(minDistance, 
                    maxDistance, 
                    difficultySetting * TARGETSPEEDSCALE, 
                    difficultySetting / 6F );
            return gen;
        }
        
        #endregion

        void Update()
        {
            if (inWave && activeAnchors < 5)
            {
                _anchorOn++;
                if (_anchorOn < _anchorCount)
                    for (int i = 0; i < 5; i++)
                    {
                        if (_activeAnchor[i] == null)
                        {
                            GameObject t = _getAnchor.getObject();
                            _activeAnchor[i] = t;
                            Anchor curr = t.GetComponent<Anchor>();
                            curr.setInfo(_anchorList[_anchorOn]);
                            t.SetActive(true);
                            break;
                        }
                    }
            }
            if (timeCounter > Time.time)
            {
                timeCounter = Time.time + TIMESCALE;
                MovementManager.eccentricityModifier = Random.insideUnitSphere;
            }
        }



        public void despawnAllAnchors()
        {
            _getAnchor.despawnAllObjects();
        }


      
        int activeAnchors
        {
            get
            {
                int i = 0;
                foreach (GameObject a in _activeAnchor) { if (a != null ? (a.GetComponent<Anchor>() is Anchor) : false) i++; }
                return i;
            }
        }

        private void Start()
        {
            timeCounter = Time.time;
            _activeAnchor = new GameObject[5];
            _anchorList = new AnchorInfo[5];
            _getAnchor = gameObject.AddComponent<ObjectPool>();
            _getAnchor.objectPrefab = Resources.Load("Prefabs/TargetPrefabs/Anchor") as GameObject;
            initializeWave(100.002f, 1);
        }
    }
}