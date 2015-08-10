using System;

namespace SharpNeat.DomainsExtra.MazeNavigation.Components
{
    //circle class, does some basic trig
    public class Circle2D
    {
        public Point2D p;
        public double Radius;

        public Circle2D()
        {
        }

        public Circle2D(Circle2D other)
        {
            p = other.p;
            Radius = other.Radius;
        }

        public Circle2D(Point2D a, double rad)
        {
            Radius = rad;
            p = a;
        }

        //test if circle collides with other circle
        //used in robot collision detection
        public bool Collide(Circle2D other)
        {
            var dx = other.p.x - p.x;
            var dy = other.p.y - p.y;
            dx *= dx;
            dy *= dy;
            var radSum = other.Radius + Radius;
            radSum *= radSum;
            return (dx + dy) < radSum;
        }
    }

    //point class, does some basic trig
    public class Point2D
    {
        public double x, y;

        public Point2D()
        {
        }

        public Point2D(double a, double b)
        {
            x = a;
            y = b;
        }

        public Point2D(Point2D another)
        {
            x = another.x;
            y = another.y;
        }

        //calculate angle of line from origin to this point
        public double Angle()
        {
            if (x == 0.0)
            {
                if (y > 0)
                    return 3.14/2.0;
                return 3.14*3.0/2.0;
            }
            var ang = Math.Atan(y/x);
            if (x > 0)
                return ang;

            return ang + 3.14;
        }

        //rotate this point about another point
        public void Rotate(double angle, Point2D point)
        {
            x -= point.x;
            y -= point.y;

            var ox = x;
            var oy = y;

            x = Math.Cos(angle)*ox - Math.Sin(angle)*oy;
            y = Math.Sin(angle)*ox + Math.Cos(angle)*oy;

            x += point.x;
            y += point.y;
        }

        //what is the squared dist to another point
        public double distance_sq(Point2D point)
        {
            var dx = point.x - x;
            var dy = point.y - y;
            return dx*dx + dy*dy;
        }

        public double ManhattanDistance(Point2D point)
        {
            var dx = Math.Abs(point.x - x);
            var dy = Math.Abs(point.y - y);
            return dx + dy;
        }

        //what is the distance to another point
        public double Distance(Point2D point)
        {
            return Math.Sqrt(distance_sq(point));
        }
    }

    //line segment class, does some basic trig
    public class Line2D
    {
        public Point2D P1, P2;

        public Line2D()
        {
        }

        public Line2D(Point2D a, Point2D b)
        {
            P1 = a;
            P2 = b;
        }

        public Line2D(Line2D other)
        {
            P1 = other.P1;
            P2 = other.P2;
        }

        public void Scale(double factor)
        {
            var mid = Midpoint();
            P1.x = mid.x + (P1.x - mid.x)*factor;
            P1.y = mid.y + (P1.y - mid.y)*factor;
            P2.x = mid.x + (P2.x - mid.x)*factor;
            P2.y = mid.y + (P2.y - mid.y)*factor;
        }

        //what is the midpoint of this line
        public Point2D Midpoint()
        {
            var x = (P1.x + P2.x)/2.0;
            var y = (P1.y + P2.y)/2.0;
            return new Point2D(x, y);
        }

        //calculate the nearest intersection of this line
        //with a circle -- if the line is interpreted as a ray
        //going from its first endpoint to the second
        public double nearest_intersection(Circle2D C, out bool found)
        {
            var dx = P2.x - P1.x;
            var dy = P2.y - P1.y;

            var px = P1.x - C.p.x;
            var py = P1.y - C.p.y;

            var a = dx*dx + dy*dy;
            var b = 2*px*dx + 2*py*dy;
            var c = px*px + py*py - C.Radius*C.Radius;

            var det = b*b - 4.0*a*c;

            if (det < 0.0)
            {
                found = false;
                return -1.0;
            }

            var sqrtDet = Math.Sqrt(det);
            var t1 = (-b + sqrtDet)/(2*a);
            var t2 = (-b - sqrtDet)/(2*a);

            found = false;
            var t = 0.0;
            if (t2 < 0)
            {
                if (t1 > 0)
                {
                    found = true;
                    t = t1;
                }
            }
            else
            {
                found = true;
                t = t2;
            }
            if (!found)
                return -1.0;

            return t*Math.Sqrt(dx*dx + dy*dy);
        }

        //calculate the point of intersection between two line segments
        public Point2D Intersection(Line2D L, out bool found)
        {
            var pt = new Point2D(0.0, 0.0);
            var a = P1;
            var b = P2;
            var c = L.P1;
            var d = L.P2;

            var rTop = (a.y - c.y)*(d.x - c.x) - (a.x - c.x)*(d.y - c.y);
            var rBot = (b.x - a.x)*(d.y - c.y) - (b.y - a.y)*(d.x - c.x);

            var sTop = (a.y - c.y)*(b.x - a.x) - (a.x - c.x)*(b.y - a.y);
            var sBot = (b.x - a.x)*(d.y - c.y) - (b.y - a.y)*(d.x - c.x);

            if ((rBot == 0 || sBot == 0))
            {
                found = false;
                return pt;
            }
            var r = rTop/rBot;
            var s = sTop/sBot;
            if ((r > 0) && (r < 1) && (s > 0) && (s < 1))
            {
                pt.x = a.x + r*(b.x - a.x);
                pt.y = a.y + r*(b.y - a.y);
                found = true;
                return pt;
            }
            found = false;
            return pt;
        }

        //what is the squared distance from this line to a point 
        public double distance_sq(Point2D n)
        {
            var utop = (n.x - P1.x)*(P2.x - P1.x) + (n.y - P1.y)*(P2.y - P1.y);
            var ubot = P1.distance_sq(P2);
            var u = utop/ubot;

            if (u < 0 || u > 1)
            {
                var d1 = P1.distance_sq(n);
                var d2 = P2.distance_sq(n);
                if (d1 < d2) return d1;
                return d2;
            }
            var p = new Point2D(0.0, 0.0) {x = P1.x + u*(P2.x - P1.x), y = P1.y + u*(P2.y - P1.y)};
            return p.distance_sq(n);
        }

        //what is the distance from this line to a point
        public double Distance(Point2D n)
        {
            return Math.Sqrt(distance_sq(n));
        }

        //what is the squared magnitude of this line segment
        public double length_sq()
        {
            return P1.distance_sq(P2);
        }

        //what is the length of this line segment
        public double Length()
        {
            return P1.Distance(P2);
        }
    }
}