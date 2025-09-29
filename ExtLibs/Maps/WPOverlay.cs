using GMap.NET;
using GMap.NET.WindowsForms;
using GMap.NET.WindowsForms.Markers;
using MissionPlanner.Controls.Waypoints;
using MissionPlanner;
using MissionPlanner.Maps;
using MissionPlanner.Utilities;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Diagnostics;

namespace MissionPlanner.ArduPilot
{
    public class WPOverlay
    {
        public GMapOverlay overlay = new GMapOverlay("WPOverlay");

        // 连接后可读取的速度参数（单位：m/s）
        public float? FlightSpeedMS { get; private set; }
        public float? ClimbSpeedMS { get; private set; }
        public float? DescentSpeedMS { get; private set; }

        // 全局最近一次已知的飞行速度（用于其他视图复用）
        public static float? LastKnownFlightSpeedMS { get; private set; }

        // 当前 Home（用于计算距离与时间）
        private PointLatLngAlt _currentHome = PointLatLngAlt.Zero;
        
        // 任务中的速度变化历史（索引 -> 速度 m/s）
        private Dictionary<int, float> _missionSpeedChanges = new Dictionary<int, float>();
        // 航点标签到任务索引的映射（用于根据航点回溯最近的速度变更）
        private Dictionary<string, int> _wpTagToMissionIndex = new Dictionary<string, int>();

        /// <summary>
        /// 通过航点在地图上的数字标签，获取其对应的任务行索引（0-based）。
        /// 返回 null 表示未找到映射，调用方可退回旧逻辑。
        /// </summary>
        public int? GetMissionIndexByTag(string tag)
        {
            if (string.IsNullOrEmpty(tag))
                return null;

            if (_wpTagToMissionIndex.TryGetValue(tag, out var idx))
                return idx;

            return null;
        }

        /// <summary>
        /// list of points as per the mission
        /// </summary>
        public List<PointLatLngAlt> pointlist = new List<PointLatLngAlt>();
        /// <summary>
        /// list of point as per mission including jump repeats
        /// </summary>
        List<PointLatLngAlt> route = new List<PointLatLngAlt>();

        /// <summary>
        /// 刷新飞行器速度相关参数（需已连接）。
        /// </summary>
        /// <param name="isConnected">是否已连接到飞行器</param>
        /// <param name="param">飞行器参数字典</param>
        public void RefreshSpeedParams(bool isConnected, dynamic param = null)
        {
            try
            {
                if (!isConnected || param == null)
                {
                    // Debug.WriteLine($"[WPOverlay] RefreshSpeedParams skipped: isConnected={isConnected}, param={(param == null ? "null" : "ok")}");
                    return;
                }

                FlightSpeedMS = null;
                ClimbSpeedMS = null;
                DescentSpeedMS = null;

                // 飞行速度：优先取地速（WPNAV_SPEED / WP_SPEED_MAX），否则退回空速（TRIM_ARSPD_CM）
                if (param.ContainsKey("WPNAV_SPEED"))
                {
                    try { FlightSpeedMS = (float)param["WPNAV_SPEED"] / 100.0f; } catch { try { FlightSpeedMS = ((dynamic)param["WPNAV_SPEED"]).float_value / 100.0f; } catch { } }
                }
                if (FlightSpeedMS == null && param.ContainsKey("WP_SPEED_MAX"))
                {
                    try { FlightSpeedMS = (float)param["WP_SPEED_MAX"] / 100.0f; } catch { try { FlightSpeedMS = ((dynamic)param["WP_SPEED_MAX"]).float_value / 100.0f; } catch { } }
                }
                if (FlightSpeedMS == null && param.ContainsKey("TRIM_ARSPD_CM"))
                {
                    try { FlightSpeedMS = (float)param["TRIM_ARSPD_CM"] / 100.0f; } catch { try { FlightSpeedMS = ((dynamic)param["TRIM_ARSPD_CM"]).float_value / 100.0f; } catch { } }
                }

                // 记录为全局最近一次速度，供其他视图使用
                LastKnownFlightSpeedMS = FlightSpeedMS ?? LastKnownFlightSpeedMS;

                // 垂直速度：WPNAV_SPEED_UP / WPNAV_SPEED_DN（cm/s）
                if (param.ContainsKey("WPNAV_SPEED_UP"))
                {
                    try { ClimbSpeedMS = (float)param["WPNAV_SPEED_UP"] / 100.0f; } catch { try { ClimbSpeedMS = ((dynamic)param["WPNAV_SPEED_UP"]).float_value / 100.0f; } catch { } }
                }
                if (param.ContainsKey("WPNAV_SPEED_DN"))
                {
                    try { DescentSpeedMS = (float)param["WPNAV_SPEED_DN"] / 100.0f; } catch { try { DescentSpeedMS = ((dynamic)param["WPNAV_SPEED_DN"]).float_value / 100.0f; } catch { } }
                }

                // Debug.WriteLine($"[WPOverlay] Speed params => FLT={FlightSpeedMS?.ToString("0.00") ?? "null"} m/s, UP={ClimbSpeedMS?.ToString("0.00") ?? "null"} m/s, DN={DescentSpeedMS?.ToString("0.00") ?? "null"} m/s");
            }
            catch
            {
                // 忽略读取失败，保持为 null
                // Debug.WriteLine($"[WPOverlay] RefreshSpeedParams failed: isConnected={isConnected}, param={(param == null ? "null" : "ok")}");
            }
        }

        public void CreateOverlay(PointLatLngAlt home, List<Locationwp> missionitems, double wpradius, double loiterradius, double altunitmultiplier)
        {
            overlay.Clear();
            _missionSpeedChanges.Clear(); // 清空速度变化历史
            _wpTagToMissionIndex.Clear();

            GMapPolygon fencepoly = null;

            double maxlat = -180;
            double maxlong = -180;
            double minlat = 180;
            double minlong = 180;

            int dolandstart = -1;
            
            // 用于重新排序航点数字标识的计数器
            int waypointCounter = 1;

            Func<MAVLink.MAV_FRAME, double, double, double> gethomealt = (altmode, lat, lng) =>
                GetHomeAlt(altmode, home.Alt, lat, lng);

            if (home != PointLatLngAlt.Zero)
            {
                _currentHome = home;
                home.Tag = "H";
                pointlist.Add(home);
                route.Add(pointlist[pointlist.Count - 1]);
                addpolygonmarker("H", home.Lng, home.Lat, home.Alt * altunitmultiplier, null, 0);
            }

            for (int a = 0; a < missionitems.Count; a++)
            {
                var item = missionitems[a];
                var itemnext = a + 1 < missionitems.Count ? missionitems[a + 1] : default(Locationwp);

                ushort command = item.id;

                // invalid locationwp
                if (command == 0)
                {
                    pointlist.Add(null);
                    continue;
                }

                // navigatable points
                if (command < (ushort) MAVLink.MAV_CMD.LAST &&
                    command != (ushort) MAVLink.MAV_CMD.RETURN_TO_LAUNCH &&
                    command != (ushort) MAVLink.MAV_CMD.CONTINUE_AND_CHANGE_ALT &&
                    command != (ushort) MAVLink.MAV_CMD.DELAY &&
                    command != (ushort) MAVLink.MAV_CMD.GUIDED_ENABLE
                    || command == (ushort) MAVLink.MAV_CMD.DO_SET_ROI || command == (ushort)MAVLink.MAV_CMD.DO_LAND_START)
                {
                    // land can be 0,0 or a lat,lng
                    if ((command == (ushort)MAVLink.MAV_CMD.LAND || command == (ushort)MAVLink.MAV_CMD.VTOL_LAND) && item.lat == 0 && item.lng == 0)
                    {
                        continue;
                    }

                    if (command == (ushort) MAVLink.MAV_CMD.DO_LAND_START && item.lat != 0 && item.lng != 0)
                    {     
                        pointlist.Add(new PointLatLngAlt(item.lat, item.lng,
                            item.alt + gethomealt((MAVLink.MAV_FRAME) item.frame, item.lat, item.lng),
                            waypointCounter.ToString()));
                        route.Add(pointlist[pointlist.Count - 1]);

                        dolandstart = a;
                        // draw everything before
                        if (route.Count > 0)
                        {
                            RegenerateWPRoute(route, home, false);
                            route.Clear();
                        }
                        
                        route.Add(pointlist[pointlist.Count - 1]);
                        _wpTagToMissionIndex[waypointCounter.ToString()] = a;
                        addpolygonmarker(waypointCounter.ToString(), item.lng, item.lat,
                            item.alt * altunitmultiplier, null, wpradius);
                        waypointCounter++;
                    } 
                    else if ((command == (ushort) MAVLink.MAV_CMD.LAND || command == (ushort) MAVLink.MAV_CMD.VTOL_LAND) && item.lat != 0 && item.lng != 0)
                    {
                        pointlist.Add(new PointLatLngAlt(item.lat, item.lng,
                            item.alt + gethomealt((MAVLink.MAV_FRAME) item.frame, item.lat, item.lng),
                            waypointCounter.ToString()));
                        route.Add(pointlist[pointlist.Count - 1]);
                        _wpTagToMissionIndex[waypointCounter.ToString()] = a;
                        addpolygonmarker(waypointCounter.ToString(), item.lng, item.lat,
                            item.alt * altunitmultiplier, null, wpradius);
                        waypointCounter++;

                        RegenerateWPRoute(route, home,  false);
                        route.Clear();
                    } 
                    else if (command == (ushort) MAVLink.MAV_CMD.DO_SET_ROI)
                    {
                        pointlist.Add(new PointLatLngAlt(item.lat, item.lng,
                                item.alt + gethomealt((MAVLink.MAV_FRAME) item.frame, item.lat, item.lng),
                                "ROI" + waypointCounter)
                            {color = Color.Red});
                        // do set roi is not a nav command. so we dont route through it
                        //fullpointlist.Add(pointlist[pointlist.Count - 1]);
                        GMarkerGoogle m =
                            new GMarkerGoogle(new PointLatLng(item.lat, item.lng),
                                GMarkerGoogleType.red);
                        m.ToolTipMode = MarkerTooltipMode.Always;
                        m.ToolTipText = waypointCounter.ToString();
                        m.Tag = waypointCounter.ToString();
                        waypointCounter++;

                        GMapMarkerRect mBorders = new GMapMarkerRect(m.Position);
                        {
                            mBorders.InnerMarker = m;
                            mBorders.Tag = "Dont draw line";
                        }

                        // check for clear roi, and hide it
                        if (m.Position.Lat != 0 && m.Position.Lng != 0)
                        {
                            // order matters
                            overlay.Markers.Add(m);
                            overlay.Markers.Add(mBorders);
                        }
                    }
                    else if (command == (ushort) MAVLink.MAV_CMD.LOITER_TIME ||
                             command == (ushort) MAVLink.MAV_CMD.LOITER_TURNS ||
                             command == (ushort) MAVLink.MAV_CMD.LOITER_TO_ALT ||
                             command == (ushort) MAVLink.MAV_CMD.LOITER_UNLIM)
                    {
                        if (item.lat == 0 && item.lng == 0)
                        {
                            pointlist.Add(null);
                            // loiter at current location.
                            if (route.Count >= 1)
                            {
                                var lastpnt = route[route.Count - 1];
                                //addpolygonmarker((a + 1).ToString(), lastpnt.Lng, lastpnt.Lat,item.alt, Color.LightBlue, loiterradius);
                            }
                        }
                        else
                        {
                            pointlist.Add(new PointLatLngAlt(item.lat, item.lng,
                                item.alt + gethomealt((MAVLink.MAV_FRAME) item.frame, item.lat, item.lng),
                                waypointCounter.ToString())
                            {
                                color = Color.LightBlue
                            });

                            // Calculate the loiter radius and direction for this command, if one is specified
                            var this_loiterradius = loiterradius;
                            if (command == (ushort)MAVLink.MAV_CMD.LOITER_TURNS ||
                                command == (ushort)MAVLink.MAV_CMD.LOITER_UNLIM)
                            {
                                this_loiterradius = item.p3 != 0 ? item.p3 : this_loiterradius;
                            }
                            else if (command == (ushort)MAVLink.MAV_CMD.LOITER_TO_ALT)
                            {
                                this_loiterradius = item.p2 != 0 ? item.p2 : this_loiterradius;
                            }
                            int loiterdirection = Math.Sign(this_loiterradius);
                            this_loiterradius = Math.Abs(this_loiterradius);

                            // exit at tangent
                            if (item.p4 == 1)
                            {
                                var from = pointlist.Last();
                                var to = itemnext.lat != 0 && itemnext.lng != 0
                                    ? new PointLatLngAlt(itemnext)
                                    {
                                        Alt = itemnext.alt + gethomealt((MAVLink.MAV_FRAME) item.frame, item.lat,
                                                  item.lng)
                                    }
                                    : from;

                                var bearing = from.GetBearing(to);
                                var dist = from.GetDistance(to);

                                if (dist > this_loiterradius)
                                {
                                    route.Add(pointlist[pointlist.Count - 1]);
                                    var theta = Math.Acos(this_loiterradius / dist) * MathHelper.rad2deg;
                                    var offset = from.newpos(bearing - loiterdirection*theta, this_loiterradius);
                                    route.Add(offset);
                                }
                                else
                                {
                                    route.Add(pointlist[pointlist.Count - 1]);
                                }
                            }
                            else
                                route.Add(pointlist[pointlist.Count - 1]);

                            _wpTagToMissionIndex[waypointCounter.ToString()] = a;
                            addpolygonmarker(waypointCounter.ToString(), item.lng, item.lat,
                                item.alt * altunitmultiplier, Color.LightBlue, this_loiterradius);
                            waypointCounter++;
                        }
                    }
                    else if (command == (ushort) MAVLink.MAV_CMD.SPLINE_WAYPOINT)
                    {
                        pointlist.Add(new PointLatLngAlt(item.lat, item.lng,
                                item.alt + gethomealt((MAVLink.MAV_FRAME) item.frame, item.lat, item.lng),
                                waypointCounter.ToString())
                            {Tag2 = "spline"});
                        route.Add(pointlist[pointlist.Count - 1]);
                        _wpTagToMissionIndex[waypointCounter.ToString()] = a;
                        addpolygonmarker(waypointCounter.ToString(), item.lng, item.lat,
                            item.alt * altunitmultiplier, Color.Green, wpradius);
                        waypointCounter++;
                    }
                    else if (command == (ushort) MAVLink.MAV_CMD.WAYPOINT && item.lat == 0 && item.lng == 0)
                    {
                        if(pointlist.Count > 0)
                            route.Add(pointlist[pointlist.Count - 1]);
                        pointlist.Add(null);
                    }
                    else
                    {
                        if (item.lat != 0 && item.lng != 0)
                        {
                            pointlist.Add(new PointLatLngAlt(item.lat, item.lng,
                                item.alt + gethomealt((MAVLink.MAV_FRAME) item.frame, item.lat, item.lng),
                                waypointCounter.ToString()));
                            route.Add(pointlist[pointlist.Count - 1]);
                            _wpTagToMissionIndex[waypointCounter.ToString()] = a;
                            addpolygonmarker(waypointCounter.ToString(), item.lng, item.lat,
                                item.alt * altunitmultiplier, null, wpradius);
                            waypointCounter++;
                        }
                        else
                        {
                            pointlist.Add(null);
                        }
                    }

                    maxlong = Math.Max(item.lng, maxlong);
                    maxlat = Math.Max(item.lat, maxlat);
                    minlong = Math.Min(item.lng, minlong);
                    minlat = Math.Min(item.lat, minlat);
                }
                else if (command == (ushort)MAVLink.MAV_CMD.DO_CHANGE_SPEED) // 记录速度变化
                {
                    pointlist.Add(null);
                    // p1 是速度值（m/s）
                    if (item.p2 > 0)
                    {
                        _missionSpeedChanges[a] = (float)item.p2;
                    }
                }
                else if (command == (ushort)MAVLink.MAV_CMD.DO_JUMP) // fix do jumps into the future
                {
                    pointlist.Add(null);

                    int wpno = (int) Math.Max(item.p1, 0);
                    int repeat = (int)item.p2;

                    List<PointLatLngAlt> list = new List<PointLatLngAlt>();

                    // cycle through reps
                    for (int repno = repeat; repno > 0; repno--)
                    {
                        // cycle through wps
                        for (int no = wpno; no <= a; no++)
                        {
                            if (pointlist[no] != null)
                                list.Add(pointlist[no]);
                        }
                    }
                    /*
                    if (repeat == -1)
                    {
                        for (int wps = wpno; wps < missionitems.Count; wps++)
                        {
                            var newitem = missionitems[wps-1];
                            if (newitem.lat == 0 && newitem.lng == 0 && newitem.id < (ushort)MAVLink.MAV_CMD.LAST)
                                continue;
                            list.Add((PointLatLngAlt) newitem);
                            if (newitem.id == (ushort) MAVLink.MAV_CMD.LAND)
                            {
                                route.AddRange(list);
                                RegenerateWPRoute(route, home,  false);
                                route.Clear();
                                list.Clear();
                                break;
                            }
                        }
                    }
                    */
                    route.AddRange(list);
                }
                else if (command == (ushort)MAVLink.MAV_CMD.FENCE_POLYGON_VERTEX_INCLUSION) // fence
                {
                    if(fencepoly == null)
                        fencepoly = new GMapPolygon(new List<PointLatLng>(), a.ToString());
                    pointlist.Add(new PointLatLngAlt(item.lat, item.lng, 0, (a + 1).ToString()));
                    fencepoly.Points.Add(new PointLatLngAlt(item.lat, item.lng, 0, (a + 1).ToString()));
                    addpolygonmarker((a + 1).ToString(), item.lng, item.lat,
                        null, Color.Blue, 0, MAVLink.MAV_MISSION_TYPE.FENCE);
                    if (fencepoly.Points.Count == item.p1)
                    {
                        fencepoly.Fill = Brushes.Transparent;
                        fencepoly.Stroke = Pens.Pink;
                        overlay.Polygons.Add(fencepoly);
                        fencepoly = null;
                    }
                }
                else if (command == (ushort)MAVLink.MAV_CMD.FENCE_POLYGON_VERTEX_EXCLUSION) // fence
                {
                    if (fencepoly == null)
                        fencepoly = new GMapPolygon(new List<PointLatLng>(), a.ToString());
                    pointlist.Add(new PointLatLngAlt(item.lat, item.lng, 0, (a + 1).ToString()));
                    fencepoly.Points.Add(new PointLatLngAlt(item.lat, item.lng, 0, (a + 1).ToString()));
                    addpolygonmarker((a + 1).ToString(), item.lng, item.lat,null, Color.Red, 0, MAVLink.MAV_MISSION_TYPE.FENCE);
                    if (fencepoly.Points.Count == item.p1)
                    {
                        fencepoly.Fill = new SolidBrush(Color.FromArgb(30, 255, 0, 0));
                        fencepoly.Stroke = Pens.Red;
                        overlay.Polygons.Add(fencepoly);
                        fencepoly = null;
                    }
                }
                else if ( command == (ushort)MAVLink.MAV_CMD.FENCE_CIRCLE_EXCLUSION) // fence
                {
                    pointlist.Add(new PointLatLngAlt(item.lat, item.lng, 0, (a + 1).ToString()));
                    addpolygonmarker((a + 1).ToString(), item.lng, item.lat,
                        null, Color.Red, item.p1, MAVLink.MAV_MISSION_TYPE.FENCE, Color.FromArgb(30, 255, 0, 0));
                }
                else if (command == (ushort)MAVLink.MAV_CMD.FENCE_CIRCLE_INCLUSION) // fence
                {
                    pointlist.Add(new PointLatLngAlt(item.lat, item.lng, 0, (a + 1).ToString()));
                    addpolygonmarker((a + 1).ToString(), item.lng, item.lat,
                        null, Color.Blue, item.p1, MAVLink.MAV_MISSION_TYPE.FENCE);
                }
                else if (command == (ushort)MAVLink.MAV_CMD.FENCE_RETURN_POINT) // fence
                {
                    pointlist.Add(new PointLatLngAlt(item.lat, item.lng, 0, (a + 1).ToString()));
                    addpolygonmarker((a + 1).ToString(), item.lng, item.lat,
                        null, Color.Orange, 0, MAVLink.MAV_MISSION_TYPE.FENCE);
                }
                else if (command == (ushort)MAVLink.MAV_CMD.RALLY_POINT) // rally
                {
                    pointlist.Add(new PointLatLngAlt(item.lat, item.lng, 0, (a + 1).ToString()));
                    addpolygonmarker((a + 1).ToString(), item.lng, item.lat,
                        null, Color.Orange, 0, MAVLink.MAV_MISSION_TYPE.RALLY);
                }
                else
                {
                    pointlist.Add(null);
                }

                //a++;
            }

            RegenerateWPRoute(route, home);

        }

        private double GetHomeAlt(MAVLink.MAV_FRAME altmode, double homealt, double lat, double lng)
        {
            if (altmode == MAVLink.MAV_FRAME.GLOBAL_INT || altmode == MAVLink.MAV_FRAME.GLOBAL)
            {
                return 0; // for absolute we dont need to add homealt
            }

            if (altmode == MAVLink.MAV_FRAME.GLOBAL_TERRAIN_ALT_INT || altmode == MAVLink.MAV_FRAME.GLOBAL_TERRAIN_ALT)
            {
                var sralt = srtm.getAltitude(lat, lng);
                if (sralt.currenttype == srtm.tiletype.invalid)
                    return -999;
                return sralt.alt;
            }

            return homealt;
        }

        /// <summary>
        /// used to add a marker to the map display
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="lng"></param>
        /// <param name="lat"></param>
        /// <param name="alt"></param>
        /// <param name="color"></param>
        private void addpolygonmarker(string tag, double lng, double lat, double? alt, Color? color, double wpradius, MAVLink.MAV_MISSION_TYPE type = MAVLink.MAV_MISSION_TYPE.MISSION, Color? fillcolor = null)
        {
            try
            {
                PointLatLng point = new PointLatLng(lat, lng);
                GMapMarker m = null;                
                if(type == MAVLink.MAV_MISSION_TYPE.MISSION)
                {
                    m = new GMapMarkerWP(point, tag);
                    //如果是home点
                    if(tag=="H")
                    {
                        m.ToolTipMode = MarkerTooltipMode.OnMouseOver;
                    }
                    else
                    {
                        m.ToolTipMode = MarkerTooltipMode.Always;
                    }
                    m.ToolTipText = BuildWaypointTooltip(tag, lat, lng, alt);
                    m.Tag = tag;
                }
                else if (type == MAVLink.MAV_MISSION_TYPE.FENCE)
                {
                    m = new GMarkerGoogle(point, GMarkerGoogleType.blue_dot);
                    m.Tag = tag;
                }
                else if (type == MAVLink.MAV_MISSION_TYPE.RALLY)
                {
                    m = new GMapMarkerRallyPt(point);
                    if (alt.HasValue)
                    {
                        m.ToolTipMode = MarkerTooltipMode.OnMouseOver;
                        m.ToolTipText = "Alt: " + alt.Value.ToString("0");
                    }
                    m.Tag = tag;
                }

                //MissionPlanner.GMapMarkerRectWPRad mBorders = new MissionPlanner.GMapMarkerRectWPRad(point, (int)float.Parse(TXT_WPRad.Text), MainMap);
                GMapMarkerRect mBorders = new GMapMarkerRect(point);
                {
                    mBorders.InnerMarker = m;
                    mBorders.Tag = tag;
                    mBorders.wprad = wpradius;
                    if (color.HasValue)
                    {
                        mBorders.Color = color.Value;
                    }
                    if (fillcolor.HasValue)
                    {
                        mBorders.FillColor = fillcolor.Value;
                    }
                }

                overlay.Markers.Add(m);
                overlay.Markers.Add(mBorders);
            }
            catch (Exception)
            {
            }
        }

        private string BuildWaypointTooltip(string tag, double lat, double lng, double? alt)
        {
            try
            {
                string altText = alt.HasValue ? ("Alt: " + alt.Value.ToString("0")) : string.Empty;

                // 仅对任务航点（数字标签）计算距离和时间
                bool isNumeric = int.TryParse(tag, out _);

                if (!isNumeric || _currentHome == PointLatLngAlt.Zero)
                {
                    return string.IsNullOrEmpty(altText) ? tag : ($"{tag}\n{altText}");
                }

                var wp = new PointLatLngAlt(lat, lng, 0, tag);
                double meters = _currentHome.GetDistance(wp);
                string distText = FormatDistance(meters);

                string timeText = string.Empty;
                var usedSpeed = GetEffectiveSpeedAtWaypoint(tag);
                double totalSeconds = 0;
                bool hasAny = false;

                // 水平飞行时间
                if (usedSpeed.HasValue && usedSpeed.Value > 0.01f)
                {
                    totalSeconds += meters / usedSpeed.Value;
                    hasAny = true;
                }

                // 垂直爬升/下降时间（使用真实米制高度：pointlist 中的 Alt）
                double? targetAltM = GetWaypointAltMeters(tag);
                double homeAltM = _currentHome.Alt;
                if (targetAltM.HasValue)
                {
                    double delta = Math.Abs(targetAltM.Value - homeAltM);
                    if (ClimbSpeedMS.HasValue && ClimbSpeedMS.Value > 0.01f)
                    {
                        totalSeconds += delta / ClimbSpeedMS.Value; // 上升时间
                        hasAny = true;
                    }
                    if (DescentSpeedMS.HasValue && DescentSpeedMS.Value > 0.01f)
                    {
                        totalSeconds += delta / DescentSpeedMS.Value; // 下降时间
                        hasAny = true;
                    }
                }

                if (hasAny)
                {
                    timeText = FormatDuration(totalSeconds);
                }

                if (!string.IsNullOrEmpty(altText) && !string.IsNullOrEmpty(timeText))
                    return $"{tag}\n{altText}\n目的地距离: {distText}\n飞行速度:{usedSpeed?.ToString("0.00")}m/s\n上升速度:{ClimbSpeedMS?.ToString("0.00")}m/s\n下降速度:{DescentSpeedMS?.ToString("0.00")}m/s\n预计时间: {timeText}";
                if (!string.IsNullOrEmpty(altText))
                    return $"{tag}\n{altText}\n目的地距离: {distText}";
                if (!string.IsNullOrEmpty(timeText))
                    return $"{tag}\n目的地距离: {distText}\n预计时间: {timeText}";
                return $"{tag}\n目的地距离: {distText}";
            }
            catch
            {
                return tag;
            }
        }

        private double? GetWaypointAltMeters(string tag)
        {
            try
            {
                
                var p = pointlist.FirstOrDefault(x => x != null && x.Tag == tag);
                if (p != null)
                    return p.Alt; // meters
            }
            catch { }
            return null;
        }

        private float? GetEffectiveSpeedAtWaypoint(string tag)
        {
            try
            {
                if (!_wpTagToMissionIndex.TryGetValue(tag, out var wpIndex))
                    return FlightSpeedMS ?? LastKnownFlightSpeedMS;

                // 查找该航点对应的任务索引之前最近的速度变化指令
                float? missionSpeed = null;
                for (int i = wpIndex; i >= 0; i--)
                {
                    if (_missionSpeedChanges.ContainsKey(i))
                    {
                        missionSpeed = _missionSpeedChanges[i];
                        break;
                    }
                }

                // 优先使用任务中设置的速度，否则使用参数中的默认速度
                if (missionSpeed.HasValue)
                {
                    return missionSpeed.Value;
                }
                
                // 回退到参数中的速度
                return FlightSpeedMS ?? LastKnownFlightSpeedMS;
            }
            catch { }
            return null;
        }

        private static string FormatDistance(double meters)
        {
            if (meters >= 1000)
                return (meters / 1000.0).ToString("0.0") + " km";
            return meters.ToString("0") + " m";
        }

        private static string FormatDuration(double seconds)
        {
            if (seconds < 60)
                return seconds.ToString("0") + " s";
            var ts = TimeSpan.FromSeconds(seconds);
            if (ts.TotalHours >= 1)
                return string.Format("{0}h {1}m", (int)ts.TotalHours, ts.Minutes);
            return string.Format("{0}m {1}s", ts.Minutes, ts.Seconds);
        }

        private void RegenerateWPRoute(List<PointLatLngAlt> fullpointlist, PointLatLngAlt HomeLocation,
            bool includehomeroute = true)
        {
            if (fullpointlist.Count == 0)
                return;

            GMapRoute route = new GMapRoute("wp route");
            GMapRoute homeroute = new GMapRoute("home route");

            PointLatLngAlt lastpnt = fullpointlist[0];
            PointLatLngAlt lastpnt2 = fullpointlist[0];
            PointLatLngAlt lastnonspline = fullpointlist[0];
            List<PointLatLngAlt> splinepnts = new List<PointLatLngAlt>();
            List<PointLatLngAlt> wproute = new List<PointLatLngAlt>();

            // add home - this causeszx the spline to always have a straight finish
            fullpointlist.Add(fullpointlist[0]);

            for (int a = 0; a < fullpointlist.Count; a++)
            {
                if (fullpointlist[a] == null)
                    continue;

                if (fullpointlist[a].Tag2 == "spline")
                {
                    if (splinepnts.Count == 0)
                        splinepnts.Add(lastpnt);

                    splinepnts.Add(fullpointlist[a]);
                }
                else
                {
                    if (splinepnts.Count > 0)
                    {
                        List<PointLatLng> list = new List<PointLatLng>();

                        splinepnts.Add(fullpointlist[a]);

                        Spline2 sp = new Spline2(HomeLocation);

                        sp.set_wp_origin_and_destination(sp.pv_location_to_vector(lastpnt2),
                            sp.pv_location_to_vector(lastpnt));

                        sp._flags.reached_destination = true;

                        for (int no = 1; no < (splinepnts.Count - 1); no++)
                        {
                            Spline2.spline_segment_end_type segtype =
                                Spline2.spline_segment_end_type.SEGMENT_END_STRAIGHT;

                            if (no < (splinepnts.Count - 2))
                            {
                                segtype = Spline2.spline_segment_end_type.SEGMENT_END_SPLINE;
                            }

                            sp.set_spline_destination(sp.pv_location_to_vector(splinepnts[no]), false, segtype,
                                sp.pv_location_to_vector(splinepnts[no + 1]));

                            //sp.update_spline();

                            while (sp._flags.reached_destination == false)
                            {
                                float t = 1f;
                                //sp.update_spline();
                                sp.advance_spline_target_along_track(t);
                                // Console.WriteLine(sp.pv_vector_to_location(sp.target_pos).ToString());
                                list.Add(sp.pv_vector_to_location(sp.target_pos));
                            }

                            list.Add(splinepnts[no]);
                        }

                        list.ForEach(x => { wproute.Add(x); });


                        splinepnts.Clear();

                        lastnonspline = fullpointlist[a];
                    }

                    wproute.Add(fullpointlist[a]);

                    lastpnt2 = lastpnt;
                    lastpnt = fullpointlist[a];
                }
            }

            // interpolate
            //wproute = wproute.Interpolate();

            int count = wproute.Count;
            int counter = 0;
            PointLatLngAlt homepoint = new PointLatLngAlt();
            PointLatLngAlt firstpoint = new PointLatLngAlt();
            PointLatLngAlt lastpoint = new PointLatLngAlt();

            if (count > 2)
            {
                // homeroute = last, home, first
                wproute.ForEach(x =>
                {
                    counter++;
                    if (counter == 1)
                    {
                        if (includehomeroute)
                        {
                            homepoint = x;
                            return;
                        }
                    }

                    if (counter == 2)
                    {
                        firstpoint = x;
                    }

                    if (counter == count - 1)
                    {
                        lastpoint = x;
                    }

                    if (counter == count)
                    {
                        if (includehomeroute)
                        {
                            homeroute.Points.Add(lastpoint);
                            homeroute.Points.Add(homepoint);
                            homeroute.Points.Add(firstpoint);
                        }
                        return;
                    }

                    route.Points.Add(x);
                });

                homeroute.Stroke = new Pen(Color.Yellow, 2);
                // if we have a large distance between home and the first/last point, it hangs on the draw of a the dashed line.
                if (homepoint.GetDistance(lastpoint) < 5000 && homepoint.GetDistance(firstpoint) < 5000)
                    homeroute.Stroke.DashStyle = DashStyle.Dash;


                if (includehomeroute)
                {
                    overlay.Routes.Add(homeroute);
                }

                route.Stroke = new Pen(Color.Yellow, 4);
                route.Stroke.DashStyle = DashStyle.Custom;
                overlay.Routes.Add(route);
            }
        }
    }
}
