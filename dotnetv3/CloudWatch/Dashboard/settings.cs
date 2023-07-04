using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Dashboard;

public class EbsInfo
{
    public string EbsOptimizedSupport { get; set; }
    public string EncryptionSupport { get; set; }
}

public class InstanceTypes
{
    public string InstanceType { get; set; }
    public bool CurrentGeneration { get; set; }
    public bool FreeTierEligible { get; set; }
    public List<string> SupportedUsageClasses { get; set; }
    public List<string> SupportedRootDeviceTypes { get; set; }
    public bool BareMetal { get; set; }
    public string Hypervisor { get; set; }
    public ProcessorInfo ProcessorInfo { get; set; }
    public VCpuInfo VCpuInfo { get; set; }
    public MemoryInfo MemoryInfo { get; set; }
    public bool InstanceStorageSupported { get; set; }
    public EbsInfo EbsInfo { get; set; }
    public NetworkInfo NetworkInfo { get; set; }
    public PlacementGroupInfo PlacementGroupInfo { get; set; }
    public bool HibernationSupported { get; set; }
    public bool BurstablePerformanceSupported { get; set; }
    public bool DedicatedHostsSupported { get; set; }
    public bool AutoRecoverySupported { get; set; }
}

public class MemoryInfo
{
    public int SizeInMiB { get; set; }
}

public class NetworkInfo
{
    public string NetworkPerformance { get; set; }
    public int MaximumNetworkInterfaces { get; set; }
    public int Ipv4AddressesPerInterface { get; set; }
    public int Ipv6AddressesPerInterface { get; set; }
    public bool Ipv6Supported { get; set; }
    public string EnaSupport { get; set; }
}

public class PlacementGroupInfo
{
    public List<string> SupportedStrategies { get; set; }
}

public class ProcessorInfo
{
    public List<string> SupportedArchitectures { get; set; }
    public double SustainedClockSpeedInGhz { get; set; }
}

public class Settings
{
    public string dashboardName { get; set; }
    public string exampleAlarmName { get; set; }
    public string accountId { get; set; }
    public string region { get; set; }
    public List<InstanceTypes> InstanceTypes { get; set; }
}

public class VCpuInfo
{
    public int DefaultVCpus { get; set; }
    public int DefaultCores { get; set; }
    public int DefaultThreadsPerCore { get; set; }
    public List<int> ValidCores { get; set; }
    public List<int> ValidThreadsPerCore { get; set; }
}

