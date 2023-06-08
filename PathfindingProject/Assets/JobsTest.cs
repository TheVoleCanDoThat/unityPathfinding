using System.Collections;
using System.Collections.Generic;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Unity.Collections;
using Unity.Burst;

public class JobsTest : MonoBehaviour
{
    [SerializeField]
    bool usingJobs;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        float startTime = Time.realtimeSinceStartup;
        if(usingJobs)
        {
            NativeArray<JobHandle> jobHandleList = new NativeArray<JobHandle>(new JobHandle[10],Allocator.Temp);
            for (int i = 0; i < 10; i++)
            {
                JobHandle jobHandle = toughTaskJob();
                jobHandleList[i] = jobHandle;
            }

            JobHandle.CompleteAll(jobHandleList);
            jobHandleList.Dispose();
        }else
        {
            for(int i=0;i<10;i++)
            {
                toughTask();
            }
            
        }
        Debug.Log(((Time.realtimeSinceStartup - startTime)*1000f) +"ms");
    }

   
    void toughTask()
    {
        float value = 0f;
        for(int i=0;i<50000;i++)
        {
            value = math.exp10(math.sqrt(value));
        }
    }

    private JobHandle toughTaskJob()
    {
        toughJob job = new toughJob();
        return job.Schedule();
    }
   
}

[BurstCompile]
public struct toughJob : IJob
{
    public void Execute()
    {
        float value = 0f;
        for (int i = 0; i < 50000; i++)
        {
            value = math.exp10(math.sqrt(value));
        }
    }
}
