using System;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace CablePlugin.Data
{
    /// <summary>
    /// Получение геометрической длины полилинии / кривой.
    /// </summary>
    public static class PolylineLengthHelper
    {
        /// <summary>
        /// Длина объекта в единицах чертежа.
        /// Поддерживает Polyline, Polyline2d, Polyline3d, Line, Arc, Spline, Curve.
        /// </summary>
        public static double GetLength(ObjectId id, Transaction tr)
        {
            if (id.IsNull || tr == null)
                return 0;

            var obj = tr.GetObject(id, OpenMode.ForRead);

            if (obj is Polyline pl)
                return pl.Length;

            if (obj is Polyline2d pl2d)
                return GetCurveLength(pl2d);

            if (obj is Polyline3d pl3d)
                return GetCurveLength(pl3d);

            if (obj is Line line)
                return line.Length;

            if (obj is Arc arc)
                return arc.Length;

            if (obj is Spline spline)
                return GetCurveLength(spline);

            if (obj is Curve curve)
                return GetCurveLength(curve);

            return 0;
        }

        private static double GetCurveLength(Curve curve)
        {
            try
            {
                return curve.GetDistanceAtParameter(curve.EndParam)
                     - curve.GetDistanceAtParameter(curve.StartParam);
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Проверка, что объект подходит как трасса кабеля.
        /// </summary>
        public static bool IsSupportedCurve(ObjectId id, Transaction tr)
        {
            if (id.IsNull || tr == null)
                return false;

            var obj = tr.GetObject(id, OpenMode.ForRead);
            return obj is Polyline
                || obj is Polyline2d
                || obj is Polyline3d
                || obj is Line
                || obj is Arc
                || obj is Spline
                || obj is Curve;
        }
    }
}
