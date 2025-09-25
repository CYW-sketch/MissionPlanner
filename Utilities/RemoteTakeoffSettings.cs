using System;
using System.Configuration;
using System.Globalization;

namespace MissionPlanner.Utilities
{
	/// <summary>
	/// 异地起降参数的用户范围设置（持久化到 user.config）
	/// </summary>
	internal sealed class RemoteTakeoffSettings : ApplicationSettingsBase
	{
		private static readonly Lazy<RemoteTakeoffSettings> _instance =
			new Lazy<RemoteTakeoffSettings>(() => new RemoteTakeoffSettings());

		public static RemoteTakeoffSettings Instance => _instance.Value;

		[UserScopedSetting]
		[DefaultSettingValue("23.2252957")]
		public double DestLat
		{
			get { return (double)this[nameof(DestLat)]; }
			set { this[nameof(DestLat)] = value; }
		}

		[UserScopedSetting]
		[DefaultSettingValue("113.03509")]
		public double DestLng
		{
			get { return (double)this[nameof(DestLng)]; }
			set { this[nameof(DestLng)] = value; }
		}

		[UserScopedSetting]
		[DefaultSettingValue("30")]
		public double DestAlt
		{
			get { return (double)this[nameof(DestAlt)]; }
			set { this[nameof(DestAlt)] = value; }
		}

		[UserScopedSetting]
		[DefaultSettingValue("Slow")]
		public string SpeedMode
		{
			get { return (string)this[nameof(SpeedMode)]; }
			set { this[nameof(SpeedMode)] = value; }
		}

		[UserScopedSetting]
		[DefaultSettingValue("PassThrough")]
		public string LandingMode
		{
			get { return (string)this[nameof(LandingMode)]; }
			set { this[nameof(LandingMode)] = value; }
		}

		[UserScopedSetting]
		[DefaultSettingValue("5")]
		public double CargoDelaySec
		{
			get { return (double)this[nameof(CargoDelaySec)]; }
			set { this[nameof(CargoDelaySec)] = value; }
		}

		[UserScopedSetting]
		[DefaultSettingValue("10")]
		public double AirDropHeight
		{
			get { return (double)this[nameof(AirDropHeight)]; }
			set { this[nameof(AirDropHeight)] = value; }
		}

		[UserScopedSetting]
		[DefaultSettingValue("true")]
		public bool TerrainFollowing
		{
			get { return (bool)this[nameof(TerrainFollowing)]; }
			set { this[nameof(TerrainFollowing)] = value; }
		}

		[UserScopedSetting]
		[DefaultSettingValue("false")]
		public bool WriteWaypoints
		{
			get { return (bool)this[nameof(WriteWaypoints)]; }
			set { this[nameof(WriteWaypoints)] = value; }
		}

		public void SaveSafe()
		{
			try
			{
				Save();
			}
			catch
			{
				// 忽略保存异常，避免影响主流程
			}
		}
	}
}


